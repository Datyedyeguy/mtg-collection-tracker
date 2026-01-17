using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Service Registration (Dependency Injection)
// ---------------------------------------------------------------------------

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
// Middleware Pipeline (order matters!)
// ---------------------------------------------------------------------------

// CORS must come early in the pipeline, before auth and endpoints
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
