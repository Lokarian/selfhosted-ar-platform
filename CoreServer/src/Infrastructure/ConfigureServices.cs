using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Enums;
using CoreServer.Infrastructure.Files;
using CoreServer.Infrastructure.Identity;
using CoreServer.Infrastructure.Persistence;
using CoreServer.Infrastructure.Persistence.Interceptors;
using CoreServer.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreServer.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        if (configuration.GetValue<bool>("UseInMemoryDatabase"))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("ARPlatformDb"));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        }

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ApplicationDbContextInitialiser>();
        services.AddSignalR()
            .AddMessagePackProtocol();

        services.AddIdentity<AppIdentityUser, IdentityRole>(config =>
        {
            config.Lockout.MaxFailedAccessAttempts = 10;
            config.SignIn.RequireConfirmedEmail = false;
        }).AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddTransient<IDateTime, DateTimeService>();
        services.AddTransient<IIdentityService, IdentityService>();
        services.AddTransient<ICsvFileBuilder, CsvFileBuilder>();
        services.AddTransient<ITokenService, JWTTokenService>();
        services.AddTransient<IFileStorageService, FileStorage>();
        services.AddAuthentication(); //todo necessary?

        services.AddAuthorization(options =>
            options.AddPolicy("CanPurge", policy => policy.RequireRole("Administrator")));

        return services;
    }
}