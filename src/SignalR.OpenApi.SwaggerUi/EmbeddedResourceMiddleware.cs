// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace SignalR.OpenApi.SwaggerUi;

/// <summary>
/// Middleware for serving embedded SignalR OpenAPI resources (JS, CSS).
/// </summary>
internal static class EmbeddedResourceMiddleware
{
    private static readonly Assembly ResourceAssembly = typeof(EmbeddedResourceMiddleware).Assembly;

    /// <summary>
    /// Configures the application to serve embedded SignalR OpenAPI resources.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="requestPath">The request path prefix for serving resources.</param>
    /// <returns>The application builder for chaining.</returns>
    internal static IApplicationBuilder UseSignalROpenApiResources(
        this IApplicationBuilder app,
        string requestPath)
    {
        var fileProvider = new EmbeddedFileProvider(ResourceAssembly, string.Empty);

        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = new PathString(requestPath),
            FileProvider = fileProvider,
        });

        return app;
    }
}
