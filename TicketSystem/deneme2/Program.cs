using TicketSystem.Data;
using TicketSystem.Interfaces;
using TicketSystem.Models;
using TicketSystem.Repository;
using TicketSystem.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
     options.AddPolicy("AllowSpecificOrigin",
         policy =>
         {
              policy.WithOrigins("http://localhost:5173")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
         });
});
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(option =>
{
     option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
     option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
     {
          In = ParameterLocation.Header,
          Description = "Please enter a valid token",
          Name = "Authorization",
          Type = SecuritySchemeType.Http,
          BearerFormat = "JWT",
          Scheme = "Bearer"
     });
     option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
     options.User.RequireUniqueEmail = true;
     options.Password.RequireDigit = true;
     options.Password.RequireLowercase = true;
     options.Password.RequireUppercase = true;
     options.Password.RequireNonAlphanumeric = true;
     options.Password.RequiredLength = 12;
}).AddEntityFrameworkStores<ApplicationDbContext>().AddRoles<IdentityRole>(); ;

builder.Services.AddAuthentication(options =>
{
     options.DefaultAuthenticateScheme =
     options.DefaultChallengeScheme =
     options.DefaultForbidScheme =
     options.DefaultScheme =
     options.DefaultSignInScheme =
     options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
     options.TokenValidationParameters = new TokenValidationParameters
     {
          ValidateIssuer = true,
          ValidIssuer = builder.Configuration["JWT:Issuer"],
          ValidateAudience = true,
          ValidAudience = builder.Configuration["JWT:Audience"],
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey
         (
             System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"])
         )
     };
});

builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserTicketRepository, UserTicketRepository>();
builder.Services.AddScoped<IFirmProductRepository, FirmProductRepository>();
builder.Services.AddScoped<IFirmRepository, FirmRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IFirmUserRepository, FirmUserRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
     app.UseSwagger();
     app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
