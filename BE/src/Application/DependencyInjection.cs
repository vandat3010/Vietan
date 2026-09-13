using System.Reflection;
using Backend.Application.Interfaces.Services;
using Backend.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Application;

/// <summary>
/// Single entry point for wiring the Application layer into the DI container.
/// Program.cs only ever calls <c>builder.Services.AddApplication()</c> - it never
/// registers an Application type directly, which keeps layer boundaries honest.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoleService, RoleService>();

        return services;
    }
}
