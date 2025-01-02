using GateKeeper.Server.Models.Account;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace GateKeeper.Server.Middleware;

public class LogEnrichmentMiddleware(RequestDelegate next, ILogger<LogEnrichmentMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<LogEnrichmentMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        Serilog.Context.LogContext.PushProperty("E_Timestamp", DateTime.UtcNow);
        // Generate or fetch correlation ID
        var correlationId = context.TraceIdentifier;
        Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId);
        var userId = int.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (userId > 0)
            Serilog.Context.LogContext.PushProperty("E_UserId", userId);
        await _next(context);
    }
}
