using System.Text;
using System.Threading.RateLimiting;
using ImageVault.Api.Middleware;
using ImageVault.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(jwtSecret)
                    ? "dev-only-insecure-fallback-key-change-me!!" // tránh crash khi thiếu cấu hình; PHẢI override qua env ở prod
                    : jwtSecret)),
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

builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p =>
{
    if (allowedOrigins.Length > 0)
        p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    else
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); // chỉ dùng khi dev (chưa cấu hình)
}));

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

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
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
