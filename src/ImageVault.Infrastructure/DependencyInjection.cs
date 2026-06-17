using ImageVault.Application.Abstractions;
using ImageVault.Application.Services;
using ImageVault.Infrastructure.Auth;
using ImageVault.Infrastructure.FreeImage;
using ImageVault.Infrastructure.Persistence;
using ImageVault.Infrastructure.Repositories;
using ImageVault.Infrastructure.Seed;
using ImageVault.Infrastructure.System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace ImageVault.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddImageVaultInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // Options (đọc từ config/env — không hardcode secret, SPEC §8).
        services.Configure<MongoOptions>(config.GetSection(MongoOptions.SectionName));
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<FreeImageOptions>(config.GetSection(FreeImageOptions.SectionName));
        services.Configure<AdminOptions>(config.GetSection(AdminOptions.SectionName));

        // Persistence
        services.AddSingleton<MongoContext>();
        services.AddSingleton<IFolderRepository, FolderRepository>();
        services.AddSingleton<IImageRepository, ImageRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();

        // System
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IIdGenerator, ObjectIdGenerator>();

        // Auth
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // FreeImage: có API key → client THẬT (HttpClient + Polly retry); rỗng → STUB (dev/no-key).
        var apiKey = config.GetSection(FreeImageOptions.SectionName)["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddSingleton<IFreeImageClient, FreeImageClientStub>();
        }
        else
        {
            services.AddHttpClient<IFreeImageClient, FreeImageClient>(http =>
                {
                    http.Timeout = TimeSpan.FromSeconds(100);
                })
                .AddPolicyHandler(HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
        }

        // Application services
        services.AddScoped<FolderService>();
        services.AddScoped<ImageService>();
        services.AddScoped<AuthService>();

        // Startup tasks: index + seed admin
        services.AddHostedService<MongoIndexInitializer>();
        services.AddHostedService<AdminSeeder>();

        return services;
    }
}
