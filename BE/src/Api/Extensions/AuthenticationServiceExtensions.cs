using System.Text;
using Backend.Infrastructure.Identity;
using Backend.Shared.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Api.Extensions;

/// <summary>
/// Wires up JWT Bearer authentication plus role/permission/policy-based
/// authorization. Kept separate from Program.cs so authentication concerns are
/// easy to find and modify in one place.
/// </summary>
public static class AuthenticationServiceExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        var jwtSettings = jwtSection.Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        var isDevelopment = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !isDevelopment;
                options.SaveToken = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    // SCADA JWT uses short claim names "role" / "username";
                    // ClaimTypes.Role is also embedded for [Authorize(Roles = "...")].
                    RoleClaimType = ScadaTokenService.RoleClaimType,
                    NameClaimType = ScadaTokenService.UsernameClaimType
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                            context.Response.Headers.Append("Token-Expired", "true");
                        return Task.CompletedTask;
                    },
                    // SignalR WebSockets cannot set Authorization header → access_token query.
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken)
                            && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        // Role-based policy (coarse-grained) for IAM admin APIs.
        // SCADA roles remain free-form strings from scada.users.role.
        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.RequireAdmin, policy => policy.RequireRole(Roles.SuperAdmin, Roles.Admin))
            .AddPolicy(Permissions.Users.View, policy => policy.RequireClaim(ClaimTypesExtended.Permission, Permissions.Users.View))
            .AddPolicy(Permissions.Users.Create, policy => policy.RequireClaim(ClaimTypesExtended.Permission, Permissions.Users.Create))
            .AddPolicy(Permissions.Users.Update, policy => policy.RequireClaim(ClaimTypesExtended.Permission, Permissions.Users.Update))
            .AddPolicy(Permissions.Users.Delete, policy => policy.RequireClaim(ClaimTypesExtended.Permission, Permissions.Users.Delete))
            .AddPolicy(Permissions.Roles.View, policy => policy.RequireClaim(ClaimTypesExtended.Permission, Permissions.Roles.View))
            .AddPolicy(Permissions.Roles.Manage, policy => policy.RequireClaim(ClaimTypesExtended.Permission, Permissions.Roles.Manage))
            .AddPolicy(Permissions.Reports.View, policy => policy.RequireClaim(ClaimTypesExtended.Permission, Permissions.Reports.View));

        return services;
    }
}
