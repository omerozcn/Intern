using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TicketSystem.Configuration;
using TicketSystem.Data;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Repository;
using TicketSystem.Security;
using TicketSystem.Service;

const string CorsPolicyName = "ConfiguredOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var configuredJwt = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(configuredJwt.Issuer)
    || string.IsNullOrWhiteSpace(configuredJwt.Audience))
{
    throw new InvalidOperationException("Jwt__Issuer and Jwt__Audience must be configured.");
}

var usesEphemeralDevelopmentKey = false;
var effectiveSigningKey = configuredJwt.SigningKey;
if (string.IsNullOrWhiteSpace(effectiveSigningKey))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Jwt__SigningKey must be supplied through environment variables or a secret store.");
    }

    effectiveSigningKey = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
    usesEphemeralDevelopmentKey = true;
}

if (Encoding.UTF8.GetByteCount(effectiveSigningKey) < 32)
{
    throw new InvalidOperationException("Jwt__SigningKey must contain at least 32 bytes.");
}

if (configuredJwt.ExpirationMinutes is < 1 or > 1440
    || configuredJwt.ClockSkewSeconds is < 0 or > 300)
{
    throw new InvalidOperationException(
        "JWT expiration must be 1-1440 minutes and clock skew must be 0-300 seconds.");
}

var frontend = builder.Configuration
    .GetSection(FrontendOptions.SectionName)
    .Get<FrontendOptions>() ?? new FrontendOptions();
if (!Uri.TryCreate(frontend.BaseUrl, UriKind.Absolute, out var frontendUri)
    || (frontendUri.Scheme != Uri.UriSchemeHttp && frontendUri.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException("Frontend__BaseUrl must be an absolute HTTP or HTTPS URL.");
}

var rateLimits = builder.Configuration
    .GetSection(RateLimitOptions.SectionName)
    .Get<RateLimitOptions>() ?? new RateLimitOptions();
if (rateLimits.GlobalPermitLimit <= 0
    || rateLimits.GlobalWindowSeconds <= 0
    || rateLimits.LoginPermitLimit <= 0
    || rateLimits.LoginWindowSeconds <= 0
    || rateLimits.PasswordPermitLimit <= 0
    || rateLimits.PasswordWindowSeconds <= 0)
{
    throw new InvalidOperationException("All rate limiting values must be positive.");
}

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];
if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("At least one Cors__AllowedOrigins origin must be configured.");
}

builder.Services
    .AddOptions<JwtOptions>()
    .Configure(options =>
    {
        builder.Configuration.GetSection(JwtOptions.SectionName).Bind(options);
        options.SigningKey = effectiveSigningKey;
    });
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<FrontendOptions>(
    builder.Configuration.GetSection(FrontendOptions.SectionName));

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions.TryAdd(
            "traceId",
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
    };
});

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request validation failed",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ticket System API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter the JWT bearer token.",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings__DefaultConnection must be configured.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 12;
        options.Password.RequiredUniqueChars = 4;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(2));

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(effectiveSigningKey));
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuredJwt.Issuer,
            ValidateAudience = true,
            ValidAudience = configuredJwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(configuredJwt.ClockSkewSeconds),
            NameClaimType = AuthClaimTypes.Name,
            RoleClaimType = AuthClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var validator = context.HttpContext.RequestServices
                    .GetRequiredService<IJwtSecurityStampValidator>();
                if (context.Principal is null
                    || !await validator.ValidateAsync(
                        context.Principal,
                        context.HttpContext.RequestAborted))
                {
                    context.Fail("The token is no longer valid.");
                }
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await WriteProblemAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Authentication required",
                    "A valid bearer token is required.",
                    context.HttpContext.RequestAborted);
            },
            OnForbidden = context => WriteProblemAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Access forbidden",
                "You do not have permission to perform this action.",
                context.HttpContext.RequestAborted)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientKey(context),
            _ => FixedWindow(rateLimits.GlobalPermitLimit, rateLimits.GlobalWindowSeconds)));
    options.AddPolicy(AuthRateLimitPolicies.Login, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientKey(context),
            _ => FixedWindow(rateLimits.LoginPermitLimit, rateLimits.LoginWindowSeconds)));
    options.AddPolicy(AuthRateLimitPolicies.Password, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientKey(context),
            _ => FixedWindow(rateLimits.PasswordPermitLimit, rateLimits.PasswordWindowSeconds)));
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        await WriteProblemAsync(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            "Too many requests",
            "Please wait before trying again.",
            cancellationToken);
    };
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IJwtSecurityStampValidator, JwtSecurityStampValidator>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IFirmProductRepository, FirmProductRepository>();
builder.Services.AddScoped<IFirmRepository, FirmRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();

var app = builder.Build();

if (usesEphemeralDevelopmentKey)
{
    app.Logger.LogWarning(
        "Jwt__SigningKey is not configured; an ephemeral development-only key is in use. Tokens will be invalid after restart.");
}

if (app.Environment.IsDevelopment())
{
    await DevelopmentDataSeeder.SeedAsync(app.Services, app.Logger);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(CorsPolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string ClientKey(HttpContext context)
{
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

static FixedWindowRateLimiterOptions FixedWindow(int permitLimit, int windowSeconds)
{
    return new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = TimeSpan.FromSeconds(windowSeconds),
        QueueLimit = 0,
        AutoReplenishment = true
    };
}

static async Task WriteProblemAsync(
    HttpContext httpContext,
    int statusCode,
    string title,
    string detail,
    CancellationToken cancellationToken)
{
    if (httpContext.Response.HasStarted)
    {
        return;
    }

    httpContext.Response.StatusCode = statusCode;
    httpContext.Response.ContentType = "application/problem+json";

    var problem = new ProblemDetails
    {
        Status = statusCode,
        Title = title,
        Detail = detail,
        Instance = httpContext.Request.Path
    };
    problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

    await JsonSerializer.SerializeAsync(
        httpContext.Response.Body,
        problem,
        new JsonSerializerOptions(JsonSerializerDefaults.Web),
        cancellationToken);
}

public partial class Program;
