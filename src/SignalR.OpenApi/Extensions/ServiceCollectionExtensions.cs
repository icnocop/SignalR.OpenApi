// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SignalR.OpenApi.Discovery;
using SignalR.OpenApi.Generation;

namespace SignalR.OpenApi.Extensions;

/// <summary>
/// Extension methods for adding SignalR OpenAPI services to the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SignalR OpenAPI document generation services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">An optional action to configure <see cref="SignalROpenApiOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSignalROpenApi(
        this IServiceCollection services,
        Action<SignalROpenApiOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<IHubDiscoverer, ReflectionHubDiscoverer>();
        services.TryAddSingleton<ISignalROpenApiDocumentGenerator, SignalROpenApiDocumentGenerator>();

        return services;
    }
}
