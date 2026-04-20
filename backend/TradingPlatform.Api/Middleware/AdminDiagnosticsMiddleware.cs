

// AdminDiagnosticsMiddleware
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TradingPlatform.Api.Middleware;

/// <summary>
/// Middleware for diagnosing admin API access issues
/// Logs request details and authorization headers
/// </summary>
public sealed class AdminDiagnosticsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminDiagnosticsMiddleware> _logger;

    public AdminDiagnosticsMiddleware(RequestDelegate next, ILogger<AdminDiagnosticsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Only log admin routes
        if (httpContext.Request.Path.StartsWithSegments("/api/admin"))
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            var method = httpContext.Request.Method;
            var path = httpContext.Request.Path;
            
            _logger.LogInformation("📍 [ADMIN REQUEST] Method: {Method}, Path: {Path}", method, path);
            _logger.LogInformation("   Authorization Header: {HasAuth}", 
                !string.IsNullOrEmpty(authHeader) ? $"✅ Present ({authHeader.Length} chars)" : "❌ MISSING");
            
            if (!string.IsNullOrEmpty(authHeader))
            {
                if (authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader["Bearer ".Length..];
                    _logger.LogInformation("   Token Length: {Length} chars, First 20: {First}...", 
                        token.Length, 
                        token.Substring(0, Math.Min(20, token.Length)));
                }
                else
                {
                    _logger.LogInformation("   ⚠️  Invalid Authorization format (not Bearer)");
                }
            }
            
            _logger.LogInformation("   User-Agent: {UserAgent}", httpContext.Request.Headers["User-Agent"]);
        }

        await _next(httpContext);

        // Log admin responses
        if (httpContext.Request.Path.StartsWithSegments("/api/admin"))
        {
            _logger.LogInformation("   📤 Response Status: {Status}", httpContext.Response.StatusCode);
            
            if (httpContext.Response.StatusCode >= 400)
            {
                _logger.LogWarning("   ❌ ERROR RESPONSE: Status {Status} for {Method} {Path}", 
                    httpContext.Response.StatusCode,
                    httpContext.Request.Method,
                    httpContext.Request.Path);
            }
        }
    }
}
