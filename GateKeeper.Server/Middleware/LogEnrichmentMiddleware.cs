namespace GateKeeper.Server.Middleware;

public class LogEnrichmentMiddleware(RequestDelegate next, ILogger<LogEnrichmentMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<LogEnrichmentMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate or fetch correlation ID
        var correlationId = context.TraceIdentifier;
        Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId);

        // If user is authenticated, add user ID
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value; // typical JWT subject
            if (!string.IsNullOrEmpty(userId))
                Serilog.Context.LogContext.PushProperty("UserId", userId);
        }

        await _next(context);
    }
}
