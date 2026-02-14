// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using SignalR.OpenApi.Models;

namespace SignalR.OpenApi.Generation;

/// <summary>
/// Generates OpenAPI documents from SignalR hub metadata.
/// </summary>
public interface ISignalROpenApiDocumentGenerator
{
    /// <summary>
    /// Generates an OpenAPI document from the discovered hub metadata.
    /// </summary>
    /// <param name="hubs">The discovered hub metadata.</param>
    /// <returns>A valid OpenAPI document describing the SignalR hubs.</returns>
    OpenApiDocument GenerateDocument(IReadOnlyList<SignalRHubInfo> hubs);
}
