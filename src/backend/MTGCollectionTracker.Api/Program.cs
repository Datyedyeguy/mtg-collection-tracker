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
using Serilog;
using Serilog.Events;

// ---------------------------------------------------------------------------
// Serilog bootstrap logger — captures startup errors before host is built.
// Replaced by the fully-configured logger once DI is ready.
// ---------------------------------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog as the logging provider, reading settings from
    // appsettings.json "Serilog" section. In production the App Insights
    // connection string is injected via APPLICATIONINSIGHTS_CONNECTION_STRING.
    builder.Host.UseSerilog((ctx, services, config) =>
        config
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "MTGCollectionTracker.Api")
            .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName));

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
        // Password requirements - prioritize length over complexity (NIST guidelines)
        // See: ADR-016 for authentication decisions
        options.Password.RequireDigit = false;              // No complexity requirements
        options.Password.RequireLowercase = false;          // No complexity requirements
        options.Password.RequireUppercase = false;          // No complexity requirements
        options.Password.RequireNonAlphanumeric = false;    // No complexity requirements
        options.Password.RequiredLength = 12;               // Length is what matters

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

    // Import job background processing
    // ManaboxCsvParser is stateless so singleton is safe — avoids a captive-dependency
    // issue since ImportWorkerService (BackgroundService = singleton) holds it directly.
    builder.Services.AddSingleton<IImportJobQueue, ImportJobQueue>();
    builder.Services.AddSingleton<IManaboxCsvParser, ManaboxCsvParser>();
    builder.Services.AddHostedService<ImportWorkerService>();

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

    // Register problem details service for RFC 9457-compliant JSON error responses.
    // app.UseExceptionHandler() below relies on this to format unhandled exceptions
    // as { "status": 500, "title": "...", "traceId": "..." } instead of an empty 500.
    builder.Services.AddProblemDetails(options =>
        options.CustomizeProblemDetails = ctx =>
            ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);

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

    // Exception handler must be first so it can catch errors from all subsequent middleware.
    // Uses the ProblemDetails service registered above to return JSON error responses.
    app.UseExceptionHandler();

    // Serilog structured request logging — logs one line per request with
    // status code, elapsed time, and request path. Must come after exception handler
    // so it captures the final status code after error handling.
    app.UseSerilogRequestLogging();

    // CORS must come before auth and endpoints
    app.UseCors("AllowFrontend");

    app.UseHttpsRedirection();

    // Authentication must come before Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
}
finally
{
    // Flush and close the Serilog logger before the process exits.
    // Essential when using Application Insights sink — without this, buffered
    // telemetry may not be sent before the process terminates.
    Log.CloseAndFlush();
}
