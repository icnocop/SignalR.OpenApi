// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Reflection;

namespace SignalR.OpenApi;

/// <summary>
/// Configuration options for SignalR OpenAPI document generation.
/// </summary>
public sealed class SignalROpenApiOptions
{
    /// <summary>
    /// Gets or sets the document name used in the URL path.
    /// Default is <c>"signalr-v1"</c>.
    /// </summary>
    public string DocumentName { get; set; } = "signalr-v1";

    /// <summary>
    /// Gets or sets the route prefix for serving the OpenAPI document.
    /// Default is <c>"openapi"</c>.
    /// The document is served at <c>/{RoutePrefix}/{DocumentName}.json</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "openapi";

    /// <summary>
    /// Gets or sets the document title.
    /// Default is <c>"SignalR Hubs"</c>.
    /// </summary>
    public string DocumentTitle { get; set; } = "SignalR Hubs";

    /// <summary>
    /// Gets or sets the document version.
    /// Default is <c>"v1"</c>.
    /// </summary>
    public string DocumentVersion { get; set; } = "v1";

    /// <summary>
    /// Gets or sets the assemblies to scan for SignalR hubs.
    /// Default is the entry assembly.
    /// </summary>
    public IList<Assembly> Assemblies { get; set; } = new List<Assembly>();

    /// <summary>
    /// Gets or sets a predicate to filter which hub types to include.
    /// Return <see langword="true"/> to include the hub; <see langword="false"/> to exclude it.
    /// Default includes all hubs.
    /// </summary>
    public Func<Type, bool>? HubFilter { get; set; }

    /// <summary>
    /// Gets or sets a predicate to filter which hub methods to include.
    /// Return <see langword="true"/> to include the method; <see langword="false"/> to exclude it.
    /// Default includes all methods.
    /// </summary>
    public Func<MethodInfo, bool>? MethodFilter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to strip the "Async" suffix from method names
    /// when generating operation IDs.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool StripAsyncSuffix { get; set; } = true;

    /// <summary>
    /// Gets or sets the hub route path map. Maps hub types to their route paths.
    /// Populated automatically from <c>MapHub&lt;T&gt;()</c> calls when using endpoint routing,
    /// or can be configured manually.
    /// </summary>
    public IDictionary<Type, string> HubRoutes { get; set; } = new Dictionary<Type, string>();
}
