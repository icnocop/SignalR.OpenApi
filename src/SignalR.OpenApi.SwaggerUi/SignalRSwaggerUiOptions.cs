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
}
