// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SignalR.OpenApi.SwaggerUi;

namespace SignalR.OpenApi.Extensions;

/// <summary>
/// Extension methods for configuring the SignalR SwaggerUI middleware.
/// </summary>
public static class SwaggerUiApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the SignalR SwaggerUI middleware to the application pipeline.
    /// Serves an interactive SwaggerUI page for SignalR hubs at
    /// <c>/{RoutePrefix}</c> (default: <c>/signalr-swagger</c>).
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configure">An optional action to further configure <see cref="SignalRSwaggerUiOptions"/>.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseSignalRSwaggerUi(
        this IApplicationBuilder app,
        Action<SignalRSwaggerUiOptions>? configure = null)
    {
        var options = app.ApplicationServices.GetService<IOptions<SignalRSwaggerUiOptions>>()?.Value
            ?? new SignalRSwaggerUiOptions();

        configure?.Invoke(options);

        var resourcePath = $"/{options.RoutePrefix.TrimStart('/')}/_resources";

        // Serve embedded JS/CSS resources
        app.UseSignalROpenApiResources(resourcePath);

        // Configure Swashbuckle SwaggerUI
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = options.RoutePrefix.TrimStart('/');
            c.SwaggerEndpoint(options.SpecUrl, options.DocumentTitle);
            c.DocumentTitle = options.DocumentTitle;

            // Inject signalr.js bundle first, then our plugin
            c.InjectJavascript($"{resourcePath}/signalr.min.js");
            c.InjectJavascript($"{resourcePath}/signalr-openapi-plugin.js");
            c.InjectStylesheet($"{resourcePath}/signalr-openapi.css");

            // Register the plugin via ConfigObject.Plugins so it is included
            // in the initial SwaggerUIBundle({...}) call (not post-initialization).
            c.ConfigObject.Plugins ??= [];
            c.ConfigObject.Plugins.Add("SignalROpenApiPlugin");

            // Pass SignalR options to the JS plugin via ConfigObject
            c.ConfigObject.AdditionalItems["signalRStripAsyncSuffix"] = options.StripAsyncSuffix;

            if (options.Headers.Count > 0)
            {
                c.ConfigObject.AdditionalItems["signalRHeaders"] = options.Headers;
            }

            // Disable syntax highlighting if configured
            if (!options.SyntaxHighlight)
            {
                c.ConfigObject.AdditionalItems["syntaxHighlight"] = false;
            }

            // Set default models expand depth
            c.DefaultModelsExpandDepth(options.DefaultModelsExpandDepth);

            // Configure auth UI for JWT Bearer (standard SwaggerUI behavior)
            c.OAuthUsePkce();
        });

        return app;
    }
}
