using System.Text;
using System.Threading.RateLimiting;
using ImageVault.Api.Middleware;
using ImageVault.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

// --- Controllers ---
builder.Services.AddControllers();

// --- ProblemDetails (RFC 7807) cho mọi lỗi (SPEC §4.3) ---
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();

// --- Infrastructure (Mongo, repos, JWT service, BCrypt, FreeImage stub, seed, index) ---
builder.Services.AddImageVaultInfrastructure(builder.Configuration);

// --- JWT Bearer auth (SPEC §7) ---
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"];
var jwtIssuer = jwtSection["Issuer"] ?? "image-vault";
var jwtAudience = jwtSection["Audience"] ?? jwtIssuer;
var jwtSigningSecret = jwtSecret;

if (string.IsNullOrWhiteSpace(jwtSigningSecret))
{
    if (!isDevelopment)
        throw new InvalidOperationException("Jwt:Secret is required outside Development.");

    jwtSigningSecret = "dev-only-insecure-fallback-key-change-me!!";
}
else if (!isDevelopment && jwtSigningSecret.Length < 32)
{
    throw new InvalidOperationException("Jwt:Secret must be at least 32 characters outside Development.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningSecret)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();

// --- Rate limit cho upload (SPEC §7): ~30 req/phút/IP ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("upload", httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

// --- Cho phép multipart lớn (ảnh ≤64MB, batch ≤20 file) ---
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 21L * 64 * 1024 * 1024; // ~1.3GB cho batch tối đa
});

// --- CORS: chỉ cho phép origin của FE (SPEC §7). Dev: nếu trống → cho phép mọi origin. ---
const string CorsPolicy = "frontend";
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?
    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();
if (allowedOrigins.Length == 0)
{
    if (!isDevelopment)
        throw new InvalidOperationException("Cors:AllowedOrigins is required outside Development.");

    allowedOrigins = new[] { "http://localhost:3000" };
}

builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p =>
{
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// --- Swagger + JWT bearer ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Image Vault API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT (không kèm chữ 'Bearer').",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (!isDevelopment)
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("X-Frame-Options", "DENY");
        headers.TryAdd("Referrer-Policy", "no-referrer");
        headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
        return Task.CompletedTask;
    });

    await next();
});

if (isDevelopment)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { } // cho integration test (Phase 2)
