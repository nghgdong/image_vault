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

        // FreeImage — STUB ở Phase 1 (client thật ở Phase 2).
        services.AddSingleton<IFreeImageClient, FreeImageClientStub>();

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
