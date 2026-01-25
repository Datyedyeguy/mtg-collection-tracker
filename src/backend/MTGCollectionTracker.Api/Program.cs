using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MTGCollectionTracker.Api.Configuration;
using MTGCollectionTracker.Api.Services;
using MTGCollectionTracker.Data;
using MTGCollectionTracker.Data.Entities;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Service Registration (Dependency Injection)
// ---------------------------------------------------------------------------

// Configure Entity Framework with PostgreSQL
// The connection string comes from appsettings.json (or appsettings.Development.json)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure ASP.NET Core Identity
// This sets up user management with our custom ApplicationUser
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password requirements - balance security with usability for a personal project
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;  // No special chars required
    options.Password.RequiredLength = 8;

    // Username requirements
    options.User.RequireUniqueEmail = true;

    // Lockout settings (protect against brute force)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<AppDbContext>()  // Use our DbContext for Identity storage
.AddDefaultTokenProviders();                // For password reset, email confirmation tokens

// Configure JWT Settings
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings not configured in appsettings.json");
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// Register JWT Service
builder.Services.AddScoped<IJwtService, JwtService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    // Set JWT Bearer as the default authentication scheme
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Validate the issuer (who created the token)
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,

        // Validate the audience (who the token is for)
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,

        // Validate the signing key
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),

        // Validate token expiration
        ValidateLifetime = true,

        // Allow some clock drift between servers
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

// Configure CORS (Cross-Origin Resource Sharing)
// This allows the frontend (running on a different port) to call our API
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)  // Only allow configured origins
              .AllowAnyMethod()              // GET, POST, PUT, DELETE, etc.
              .AllowAnyHeader()              // Accept any HTTP headers
              .AllowCredentials();           // Allow cookies/auth headers
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Database Migration (Development Only)
// ---------------------------------------------------------------------------
// Automatically apply pending migrations on startup in Development.
// This is convenient for local development but should NOT be used in production.
// Production migrations are handled by the CI/CD pipeline before deployment.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ---------------------------------------------------------------------------
// Middleware Pipeline (order matters!)
// ---------------------------------------------------------------------------

// CORS must come early in the pipeline, before auth and endpoints
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
