using System;
using Microsoft.AspNetCore.Mvc;

namespace MTGCollectionTracker.Api.Controllers;

/// <summary>
/// Health check endpoint for monitoring and load balancers.
/// Returns the current status of the API and its dependencies.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Basic health check - returns OK if the API is running.
    /// </summary>
    /// <returns>Health status with timestamp</returns>
    [HttpGet]
    public IActionResult Get()
    {
        // For now, just return that we're healthy
        // Later we'll add database connectivity checks here
        return Ok(new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}

/// <summary>
/// Response model for the health check endpoint.
/// </summary>
public class HealthResponse
{
    public required string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Version { get; set; }
}
