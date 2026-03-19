// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi.Models;

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
    /// Gets or sets the hub route path map. Maps hub types to their route paths.
    /// Populated automatically from <c>MapHub&lt;T&gt;()</c> calls when using endpoint routing,
    /// or can be configured manually.
    /// </summary>
    public IDictionary<Type, string> HubRoutes { get; set; } = new Dictionary<Type, string>();

    /// <summary>
    /// Gets or sets a value indicating whether the type discriminator property
    /// should be visible in the JSON request body examples for polymorphic
    /// sub-endpoints. When <see langword="true"/>, the discriminator appears
    /// in the JSON example but remains hidden from form-urlencoded inputs.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool IncludeDiscriminatorInExamples { get; set; } = true;

    /// <summary>
    /// Gets or sets descriptions for OpenAPI tags. Maps tag names to their
    /// descriptions, which appear in the document-level <c>tags</c> section.
    /// When a tag used by an operation is not present here, the generator
    /// falls back to the hub's XML summary if the tag name matches the hub name.
    /// </summary>
    public IDictionary<string, string> TagDescriptions { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/> used for property naming
    /// and example serialization in the generated OpenAPI document.
    /// Defaults to camelCase naming, matching ASP.NET Core's default JSON behavior.
    /// Set <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> to <see langword="null"/>
    /// for PascalCase property names.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Gets the custom header definitions that appear in the SwaggerUI Authorize dialog.
    /// Each entry is rendered as an <c>apiKey</c> security scheme (<c>in: header</c>) in the
    /// OpenAPI document, allowing users to enter header values at runtime.
    /// The key is the header name and the value is the description shown in the dialog.
    /// </summary>
    /// <example>
    /// <code>
    /// options.ApiKeyHeaders["X-Custom-Header"] = "A custom header sent with every hub connection.";
    /// </code>
    /// </example>
    public IDictionary<string, string> ApiKeyHeaders { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the security schemes to include in the OpenAPI document when hubs
    /// require authorization. Each entry is added to <c>components/securitySchemes</c>
    /// and referenced in the <c>security</c> section of operations with
    /// <c>[Authorize]</c>. When empty, no authentication scheme is emitted.
    /// </summary>
    /// <example>
    /// <code>
    /// options.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    /// {
    ///     Type = SecuritySchemeType.Http,
    ///     Scheme = "bearer",
    ///     BearerFormat = "JWT",
    ///     Description = "JWT Bearer token for SignalR hub authentication.",
    /// };
    /// </code>
    /// </example>
    public IDictionary<string, OpenApiSecurityScheme> SecuritySchemes { get; } = new Dictionary<string, OpenApiSecurityScheme>(StringComparer.Ordinal);
}
