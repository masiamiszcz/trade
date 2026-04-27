using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // ✨ PHASE 3: User Status Validation (SECURITY + BUSINESS STATE)
        // 
        // SECURITY STATE (Deleted):
        //   - Deleted users CANNOT access any endpoint
        //   - This is a hard security gate
        //   - Return 401 Unauthorized
        //
        // BUSINESS STATE (Blocked):
        //   - Blocked users CAN access endpoints
        //   - But we attach flag: HttpContext.Items["IsBlocked"] = true
        //   - Frontend/Response enrichment uses this flag for UI signaling
        //   - This prevents desync where JWT becomes stale snapshot

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                // Extract userId from claims
                var userIdClaim = httpContext.User.FindFirst("userId")?.Value ?? 
                                 httpContext.User.FindFirst("sub")?.Value;

                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    // Load user repository from DI container
                    var userRepository = httpContext.RequestServices.GetRequiredService<IUserRepository>();

                    // Load user from DB (including deleted users)
                    var user = await userRepository.GetUserByIdIncludingDeletedAsync(userId, httpContext.RequestAborted);

                    if (user != null)
                    {
                        // ========== SECURITY GATE: DELETED USER ==========
                        // Deleted users are COMPLETELY BLOCKED from accessing system
                        if (user.Status == UserStatus.Deleted)
                        {
                            _logger.LogWarning("SECURITY: Request rejected - User {UserId} is deleted", userId);
                            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await httpContext.Response.WriteAsJsonAsync(new
                            {
                                error = "Unauthorized",
                                message = "User account is no longer active."
                            });
                            return; // HARD BLOCK - request does not proceed
                        }

                        // ========== BUSINESS FLAG: BLOCKED USER ==========
                        // Blocked users CAN use the system but are flagged
                        // Response enrichment layer (or frontend) uses this to show warnings
                        if (user.Status == UserStatus.Blocked)
                        {
                            httpContext.Items["IsBlocked"] = true;
                            httpContext.Items["BlockedUntilUtc"] = user.BlockedUntilUtc;
                            httpContext.Items["BlockReason"] = user.BlockReason;
                            
                            _logger.LogInformation("REQUEST CONTEXT: User {UserId} is blocked until {BlockedUntilUtc}", 
                                userId, user.BlockedUntilUtc);
                            
                            // Request CONTINUES - no block at middleware level
                            // Frontend will show warning based on response enrichment
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in user status validation middleware");
                // Don't block request on validation error
                // This prevents middleware from breaking legitimate requests
            }
        }

        // Continue to next middleware / controller
        try
        {
            await _next(httpContext);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(httpContext, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            InvalidOperationException => HttpStatusCode.Conflict,
            _ => HttpStatusCode.InternalServerError
        };

        var title = statusCode == HttpStatusCode.InternalServerError
            ? "Internal Server Error"
            : statusCode.ToString();

        var problemDetails = new
        {
            title,
            status = (int)statusCode,
            detail = exception.Message,
            traceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        _logger.LogError(exception, "Global exception handler caught an error while processing request {Method} {Path}.",
            context.Request.Method,
            context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
