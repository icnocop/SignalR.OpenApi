// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Writers;
using SignalR.OpenApi.Discovery;
using SignalR.OpenApi.Generation;

namespace SignalR.OpenApi.Extensions;

/// <summary>
/// Extension methods for mapping the SignalR OpenAPI document endpoint.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the SignalR OpenAPI JSON document endpoint.
    /// The document is served at <c>/{RoutePrefix}/{DocumentName}.json</c>
    /// (default: <c>/openapi/signalr-v1.json</c>).
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/> for the mapped endpoint.</returns>
    public static IEndpointConventionBuilder MapSignalROpenApi(this IEndpointRouteBuilder endpoints)
    {
        PopulateHubRoutes(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<SignalROpenApiOptions>>().Value;
        var pattern = $"/{options.RoutePrefix.TrimStart('/')}/{options.DocumentName}.json";

        return endpoints.MapGet(pattern, async (HttpContext context) =>
        {
            var discoverer = context.RequestServices.GetRequiredService<IHubDiscoverer>();
            var generator = context.RequestServices.GetRequiredService<ISignalROpenApiDocumentGenerator>();

            var hubs = discoverer.DiscoverHubs();
            var document = generator.GenerateDocument(hubs);

            context.Response.ContentType = "application/json";

            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
            {
                var jsonWriter = new OpenApiJsonWriter(writer);
                document.SerializeAsV3(jsonWriter);
            }

            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(context.Response.Body);
        })
        .ExcludeFromDescription();
    }

    private static void PopulateHubRoutes(IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<SignalROpenApiOptions>>().Value;

        foreach (var dataSource in endpoints.DataSources)
        {
            foreach (var endpoint in dataSource.Endpoints)
            {
                if (endpoint is not RouteEndpoint routeEndpoint)
                {
                    continue;
                }

                var hubMetadata = routeEndpoint.Metadata.GetMetadata<HubMetadata>();
                if (hubMetadata is null)
                {
                    continue;
                }

                var hubType = hubMetadata.HubType;
                var routePattern = routeEndpoint.RoutePattern.RawText;

                // Skip negotiate endpoints (path ends with /negotiate)
                if (routePattern is not null && routePattern.EndsWith("/negotiate", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (routePattern is not null && !options.HubRoutes.ContainsKey(hubType))
                {
                    options.HubRoutes[hubType] = "/" + routePattern.TrimStart('/');
                }
            }
        }
    }
}
