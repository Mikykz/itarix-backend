using AspNetCoreRateLimit;
using Itarix.Api.Business;
using Itarix.Api.Data;
using itarixapi.Business;
using itarixapi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

namespace itarixapi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ---------- CORS: Allow only trusted frontends ----------
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MyCorsPolicy", policy =>
                {
                    policy.WithOrigins(
                        "http://127.0.0.1:5500",
                        "http://localhost:5500",
                        "https://itarix.net",
                        "https://www.itarix.net"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            });

            // ---------- Forwarded Headers for proxy/CDN ----------
            builder.Services.AddHttpContextAccessor();
            // Forwarded headers (trust Azure front-ends)
            builder.Services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                o.KnownNetworks.Clear();
                o.KnownProxies.Clear();
            });

            // ---------- JWT Authentication ----------
            var jwtKey = builder.Configuration["Jwt:Key"];
            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 16)
                throw new Exception("JWT Key is missing or too short (min 16 chars).");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.Zero,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role
                    };
                });

            builder.Services.AddAuthorization();

            // ---------- Controllers & Swagger ----------
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "itarixapi", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ---------- Rate Limiting (AspNetCoreRateLimit) ----------
            builder.Services.AddMemoryCache();
            builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
            builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
            builder.Services.AddInMemoryRateLimiting();
            builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            // ---------- ProblemDetails for cleaner error responses ----------
            builder.Services.AddProblemDetails();

            // ---------- Dependency Injection ----------
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<ITConsultationRepository>();
            builder.Services.AddScoped<ITConsultationService>();

            builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
            builder.Services.AddSingleton<PricingService>();

            builder.Services.AddScoped<IAIToolRepository, AIToolRepository>();
            builder.Services.AddScoped<IAIToolService, AIToolService>();
            builder.Services.AddScoped<IToolReviewRepository, ToolReviewRepository>();
            builder.Services.AddScoped<IToolReviewService, ToolReviewService>();
            builder.Services.AddScoped<IToolCommentRepository, ToolCommentRepository>();
            builder.Services.AddScoped<IToolCommentService, ToolCommentService>();
            builder.Services.AddScoped<IModerationRepository, ModerationRepository>();
            builder.Services.AddScoped<IModerationService, ModerationService>();

            var app = builder.Build();

            // ---------- Swagger (Dev only) ----------
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // ---------- Middleware order ----------
            app.UseHttpsRedirection();
            app.UseForwardedHeaders();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    const int durationInSeconds = 60 * 60 * 24 * 365;
                    ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={durationInSeconds}");
                }
            });

            app.UseSecurityHeaders(app.Environment); // secure headers with CSP per env
            app.UseCors("MyCorsPolicy");
            app.UseIpRateLimiting();
            app.UseAuthentication();
            app.UseAuthorization();

            // ---------- Error handling ----------
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            // ---------- Endpoints ----------
            app.MapControllers();
            // Root + health so Production doesn’t 404 at /
            app.MapGet("/", () => Results.Text("itarixapi is running"));
            app.MapGet("/health", () => Results.Ok("ok"));      // keep a plain /health
            app.MapGet("/api/health", () => Results.Ok("ok"));


            app.Run();
        }
    }
}
