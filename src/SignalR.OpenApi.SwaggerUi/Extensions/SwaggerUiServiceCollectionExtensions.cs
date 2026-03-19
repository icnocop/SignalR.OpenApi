// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using SignalR.OpenApi.SwaggerUi;

namespace SignalR.OpenApi.Extensions;

/// <summary>
/// Extension methods for registering SignalR SwaggerUI services.
/// </summary>
public static class SwaggerUiServiceCollectionExtensions
{
    /// <summary>
    /// Adds SignalR SwaggerUI services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An optional action to configure <see cref="SignalRSwaggerUiOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSignalRSwaggerUi(
        this IServiceCollection services,
        Action<SignalRSwaggerUiOptions>? configure = null)
    {
        var options = new SignalRSwaggerUiOptions();
        configure?.Invoke(options);

        services.Configure<SignalRSwaggerUiOptions>(o =>
        {
            o.RoutePrefix = options.RoutePrefix;
            o.SpecUrl = options.SpecUrl;
            o.DocumentTitle = options.DocumentTitle;
            o.UseDefaultCredentials = options.UseDefaultCredentials;
            o.StripAsyncSuffix = options.StripAsyncSuffix;
            o.SyntaxHighlight = options.SyntaxHighlight;
            o.DefaultModelsExpandDepth = options.DefaultModelsExpandDepth;

            foreach (var header in options.Headers)
            {
                o.Headers[header.Key] = header.Value;
            }
        });

        return services;
    }
}
