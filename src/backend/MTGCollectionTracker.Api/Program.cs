using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

app.UseAuthorization();

app.MapControllers();

app.Run();
