using GateKeeper.Server.Exceptions; // Added for custom exceptions
using GateKeeper.Server.Models.Common; // Added for ErrorResponse
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting; // Added for IHostEnvironment
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics; // Added for Activity
using System.Text.Json; // Added for JsonSerializer
using System.Threading.Tasks;

namespace GateKeeper.Server.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _env; // Added for environment check

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IHostEnvironment env) // Added IHostEnvironment
        {
            _next = next;
            _logger = logger;
            _env = env; // Store injected IHostEnvironment
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex) // Specific exception
            {
                _logger.LogWarning(ex, "Validation error. TraceId: {TraceId}", context.TraceIdentifier);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var response = new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = ex.Message, // Message from the exception
                    Details = _env.IsDevelopment() ? ex.ToString() : null,
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (ResourceNotFoundException ex) // Specific exception
            {
                _logger.LogWarning(ex, "Resource not found. TraceId: {TraceId}", context.TraceIdentifier);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                var response = new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = ex.Message, // Message from the exception
                    Details = _env.IsDevelopment() ? ex.ToString() : null,
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (BusinessRuleException ex) // Specific exception
            {
                _logger.LogWarning(ex, "Business rule violation. TraceId: {TraceId}", context.TraceIdentifier);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status400BadRequest; // Or StatusCodes.Status409Conflict if more appropriate
                var response = new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = ex.Message, // Message from the exception
                    Details = _env.IsDevelopment() ? ex.ToString() : null,
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (InvalidCredentialsException ex)
            {
                _logger.LogWarning(ex, "Invalid credentials. TraceId: {TraceId}", context.TraceIdentifier);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var response = new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = ex.Message,
                    Details = _env.IsDevelopment() ? ex.ToString() : null,
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (UserNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found. TraceId: {TraceId}", context.TraceIdentifier);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var response = new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = ex.Message,
                    Details = _env.IsDevelopment() ? ex.ToString() : null,
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (AccountLockedException ex)
            {
                _logger.LogWarning(ex, "Account locked. TraceId: {TraceId}", context.TraceIdentifier);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests; // Changed to 429
                var response = new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = ex.Message,
                    Details = _env.IsDevelopment() ? ex.ToString() : null,
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (InvalidTokenException ex)
            {
                _logger.LogWarning(ex, "Invalid token. TraceId: {TraceId}", context.TraceIdentifier);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var response = new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = ex.Message,
                    Details = _env.IsDevelopment() ? ex.ToString() : null,
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (RegistrationException ex)
            {
                _logger.LogWarning(ex, "Registration error. TraceId: {TraceId}", context.TraceIdentifier);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var response = new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = ex.Message,
                    Details = _env.IsDevelopment() ? ex.ToString() : null,
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (Exception ex) // Generic fallback
            {
                _logger.LogError(ex, "An unhandled exception has occurred. TraceId: {TraceId}", context.TraceIdentifier);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                var response = new ErrorResponse
                {
                    StatusCode = context.Response.StatusCode,
                    Message = "An unexpected internal server error occurred. Please try again later.",
                    Details = _env.IsDevelopment() ? ex.ToString() : null,
                    TraceId = Activity.Current?.Id ?? context.TraceIdentifier
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}
