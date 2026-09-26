using System.Reflection;

namespace ConejitoTicket.Api.Infrastructure;

// Convención: cada feature es una clase estática con clases anidadas `Handler` y `Endpoint`.
public static class FeatureRegistration
{
    private static readonly Type[] Features = typeof(FeatureRegistration).Assembly.GetTypes()
        .Where(t => t is { IsAbstract: true, IsSealed: true, DeclaringType: null }
                    && t.Namespace?.Contains(".Features") == true)
        .ToArray();

    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        foreach (var handler in Features.Select(f => f.GetNestedType("Handler")).OfType<Type>())
            services.AddScoped(handler);

        return services;
    }

    public static IEndpointRouteBuilder MapFeatures(this IEndpointRouteBuilder app)
    {
        foreach (var feature in Features)
        {
            var map = feature.GetNestedType("Endpoint")?
                .GetMethod("Map", BindingFlags.Public | BindingFlags.Static, [typeof(IEndpointRouteBuilder)]);
            map?.Invoke(null, [app]);
        }

        return app;
    }
}
