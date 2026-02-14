// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Models;

namespace SignalR.OpenApi.Discovery;

/// <summary>
/// Discovers SignalR hubs and extracts metadata for OpenAPI document generation.
/// </summary>
public interface IHubDiscoverer
{
    /// <summary>
    /// Discovers all SignalR hubs and returns their metadata.
    /// </summary>
    /// <returns>A collection of discovered hub metadata.</returns>
    IReadOnlyList<SignalRHubInfo> DiscoverHubs();
}
