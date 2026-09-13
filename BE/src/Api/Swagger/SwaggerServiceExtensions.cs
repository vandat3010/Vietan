using Microsoft.OpenApi.Models;

namespace Backend.Api.Swagger;

/// <summary>
/// Isolates all Swagger/Swashbuckle wiring so Program.cs stays declarative
/// (<c>builder.Services.AddSwaggerDocumentation()</c>).
/// </summary>
public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Backend Enterprise API",
                Version = "v1",
                Description =
                    "Reusable Clean Architecture / DDD backend template " +
                    "(ERP, SCADA, MES, CRM, HRM, Manufacturing, Warehouse).",
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter: **Bearer {your JWT token}**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    []
                }
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Backend Enterprise API v1");
            options.DocumentTitle = "Backend Enterprise API";
        });

        return app;
    }
}
