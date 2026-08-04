using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TicketSystem.Configuration;
using TicketSystem.Data;
using TicketSystem.Infrastructure;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Repositories;
using TicketSystem.Security;
using TicketSystem.Services;

const string CorsPolicyName = "ConfiguredOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// The signing key is the one setting that cannot be expressed as an attribute: an
// empty value is legal in Development, where an ephemeral key is generated instead.
var usesEphemeralDevelopmentKey = false;
var effectiveSigningKey = builder.Configuration[$"{JwtOptions.SectionName}:SigningKey"];
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

// Rate limiter partitions are built during service registration, before the options
// system is available, so this one section is still read eagerly. It is validated
// through the same ValidateOnStart pipeline below.
var rateLimits = builder.Configuration
    .GetSection(RateLimitOptions.SectionName)
    .Get<RateLimitOptions>() ?? new RateLimitOptions();

// The rate limiter partitions on the client IP. Behind a reverse proxy every request
// arrives with the proxy's address, which would collapse all callers into one bucket,
// so X-Forwarded-For is honoured only for proxies that are explicitly trusted here.
// With nothing configured the middleware stays off and the socket address is used.
var knownProxies = builder.Configuration
    .GetSection("ForwardedHeaders:KnownProxies")
    .Get<string[]>() ?? [];
var knownNetworks = builder.Configuration
    .GetSection("ForwardedHeaders:KnownNetworks")
    .Get<string[]>() ?? [];
var trustsForwardedHeaders = knownProxies.Length > 0 || knownNetworks.Length > 0;

if (trustsForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;

        // The defaults trust loopback only; replace them with the configured allowlist.
        options.KnownProxies.Clear();
        options.KnownNetworks.Clear();

        foreach (var proxy in knownProxies)
        {
            if (!IPAddress.TryParse(proxy.Trim(), out var address))
            {
                throw new InvalidOperationException(
                    $"ForwardedHeaders__KnownProxies contains '{proxy}', which is not an IP address.");
            }

            options.KnownProxies.Add(address);
        }

        foreach (var network in knownNetworks)
        {
            var parts = network.Split('/', 2);
            if (parts.Length != 2
                || !IPAddress.TryParse(parts[0].Trim(), out var prefix)
                || !int.TryParse(parts[1].Trim(), out var prefixLength))
            {
                throw new InvalidOperationException(
                    $"ForwardedHeaders__KnownNetworks contains '{network}', which is not CIDR notation.");
            }

            // Fully qualified: System.Net also has an IPNetwork in .NET 8.
            options.KnownNetworks.Add(
                new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLength));
        }
    });
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

// Every section is bound, validated and checked at startup, so a misconfigured app
// fails immediately and the object that gets validated is the object that gets
// injected — rather than a second copy read separately.
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .PostConfigure(options => options.SigningKey = effectiveSigningKey)
    .ValidateDataAnnotations()
    .Validate(
        options => Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
        "Jwt__SigningKey must contain at least 32 bytes.")
    .ValidateOnStart();

builder.Services
    .AddOptions<FrontendOptions>()
    .Bind(builder.Configuration.GetSection(FrontendOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
        "Frontend__BaseUrl must be an absolute HTTP or HTTPS URL.")
    .ValidateOnStart();

builder.Services
    .AddOptions<RateLimitOptions>()
    .Bind(builder.Configuration.GetSection(RateLimitOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Only validated when a host is configured: password reset is the sole consumer, and
// an installation that never sends mail should still start.
builder.Services
    .AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .Validate(
        options => string.IsNullOrWhiteSpace(options.Host)
            || (options.Port is > 0 and <= 65535 && !string.IsNullOrWhiteSpace(options.FromEmail)),
        "Smtp__Port must be 1-65535 and Smtp__FromEmail must be set when Smtp__Host is configured.")
    .ValidateOnStart();

builder.Services.AddExceptionHandler<UniqueConstraintExceptionHandler>();
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

    var xmlDocumentation = Path.Combine(
        AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlDocumentation))
    {
        options.IncludeXmlComments(xmlDocumentation);
    }

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

// Reads the same validated JwtOptions instance the rest of the app is injected with,
// rather than binding the section a second time.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            // Pinned so a token cannot ask to be validated with a different algorithm.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
            NameClaimType = AuthClaimTypes.Name,
            RoleClaimType = AuthClaimTypes.Role
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

// Lets an orchestrator tell "the process is up" apart from "it can reach its database".
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

builder.Services.AddSingleton<IPasswordResetNotifier, PasswordResetNotifier>();
builder.Services.AddHostedService<PasswordResetEmailDispatcher>();

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

// Seeding creates an administrator whose password is published in the README, and it
// applies migrations. Both need an explicit opt-in rather than riding on the
// environment name, so that a container started in Development stays inert by default.
if (app.Configuration.GetValue(DevelopmentDataSeeder.EnabledKey, defaultValue: false))
{
    await DevelopmentDataSeeder.SeedAsync(app.Services, app.Logger);
}

// Must run before anything reads the client address or scheme.
if (trustsForwardedHeaders)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";

    // This API only ever answers with JSON, so it needs no script, style or frame
    // privileges at all. Swagger UI does, which is why it is left out in Development.
    if (!app.Environment.IsDevelopment())
    {
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(CorsPolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness: the process answers. Readiness: it can also reach the database.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
    .AllowAnonymous();
app.MapHealthChecks("/health").AllowAnonymous();

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
