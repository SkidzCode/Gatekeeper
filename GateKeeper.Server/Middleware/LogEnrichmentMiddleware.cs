using GateKeeper.Server.Models.Account;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace GateKeeper.Server.Middleware;

public class LogEnrichmentMiddleware(RequestDelegate next, ILogger<LogEnrichmentMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<LogEnrichmentMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        using (LogContext.PushProperty("E_Timestamp", DateTime.UtcNow))
        using (LogContext.PushProperty("CorrelationId", context.TraceIdentifier))
        {
            try
            {
                var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdClaim?.Value, out int userId) && userId > 0)
                {
                    using (LogContext.PushProperty("E_UserId", userId))
                    {
                        await _next(context);
                    }
                }
                else
                {
                    await _next(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred in LogEnrichmentMiddleware.");
                throw; // Re-throw the exception to ensure it's not swallowed
            }
        }
    }
}
