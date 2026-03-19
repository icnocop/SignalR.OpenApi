// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.SwaggerUi;

/// <summary>
/// Configuration options for the SignalR SwaggerUI integration.
/// </summary>
public class SignalRSwaggerUiOptions
{
    /// <summary>
    /// Gets or sets the route prefix for the SwaggerUI page.
    /// Defaults to <c>"signalr-swagger"</c>.
    /// </summary>
    public string RoutePrefix { get; set; } = "signalr-swagger";

    /// <summary>
    /// Gets or sets the URL to the SignalR OpenAPI JSON document.
    /// Defaults to <c>"/openapi/signalr-v1.json"</c>.
    /// </summary>
    public string SpecUrl { get; set; } = "/openapi/signalr-v1.json";

    /// <summary>
    /// Gets or sets the document title displayed in the browser tab.
    /// Defaults to <c>"SignalR API"</c>.
    /// </summary>
    public string DocumentTitle { get; set; } = "SignalR API";

    /// <summary>
    /// Gets or sets a value indicating whether to use default credentials
    /// for Windows authentication. Defaults to <see langword="false"/>.
    /// </summary>
    public bool UseDefaultCredentials { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to strip the "Async" suffix
    /// from method names in the UI display. The underlying SignalR invocation
    /// always uses the real method name. Defaults to <see langword="true"/>.
    /// </summary>
    public bool StripAsyncSuffix { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether syntax highlighting is enabled
    /// in SwaggerUI response bodies. Defaults to <see langword="true"/>.
    /// </summary>
    public bool SyntaxHighlight { get; set; } = true;

    /// <summary>
    /// Gets or sets the default expand depth for models in SwaggerUI.
    /// Set to <c>-1</c> to hide the models section entirely.
    /// Defaults to <c>-1</c>.
    /// </summary>
    public int DefaultModelsExpandDepth { get; set; } = -1;

    /// <summary>
    /// Gets or sets the default expansion setting for tags and operations in SwaggerUI.
    /// Defaults to <see cref="SwaggerUi.DocExpansion.List"/> (tags expanded, operations collapsed).
    /// </summary>
    public DocExpansion DocExpansion { get; set; } = DocExpansion.List;

    /// <summary>
    /// Gets or sets a value indicating whether tags (groups) are sorted
    /// alphabetically in SwaggerUI. Defaults to <see langword="false"/>
    /// (tags appear in the order defined by the OpenAPI document).
    /// </summary>
    public bool SortTagsAlphabetically { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether operations (endpoints)
    /// within each tag are sorted alphabetically in SwaggerUI.
    /// Defaults to <see langword="false"/> (operations appear in document order).
    /// </summary>
    public bool SortOperationsAlphabetically { get; set; }

    /// <summary>
    /// Gets the custom HTTP headers to include on every SignalR hub connection.
    /// These headers are sent with the initial negotiate request and all long-polling
    /// or server-sent-events requests. WebSocket connections carry them on the
    /// initial upgrade request.
    /// </summary>
    /// <example>
    /// <code>
    /// options.Headers["X-Custom-Header"] = "value";
    /// </code>
    /// </example>
    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
