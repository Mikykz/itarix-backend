// Infrastructure/SecurityHeadersExtensions.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, IHostEnvironment env)
    {
        return app.Use(async (ctx, next) =>
        {
            var h = ctx.Response.Headers;

            // Core hardening
            h["X-Frame-Options"] = "DENY";
            h["X-Content-Type-Options"] = "nosniff";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            h["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // HSTS only in Prod (must be HTTPS)
            if (env.IsProduction())
            {
                h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
            }

            // Allow Swagger UI to run without CSP in Dev (it inlines scripts/styles)
            var isSwagger = ctx.Request.Path.StartsWithSegments("/swagger");
            if (!isSwagger)
            {
                // Content Security Policy (tuned for your frontend + CDNs you actually use)
                // If you add more CDNs later, add them here.
                var csp =
                    "default-src 'none'; " +
                    "img-src 'self' data: https:; " +
                    "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                    "script-src 'self' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                    "font-src 'self' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net data:; " +
                    "connect-src 'self' https://itarix.net https://www.itarix.net https://api.itarix.net https://localhost:7195 http://localhost:7195; " +
                    "base-uri 'self'; frame-ancestors 'none'";

                h["Content-Security-Policy"] = csp;
            }

            await next();
        });
    }
}
