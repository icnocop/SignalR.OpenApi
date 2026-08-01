// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.DocumentMetadata;

/// <summary>
/// A minimal hub, so metadata assertions are not affected by method shape.
/// </summary>
public class MetadataHub : Hub
{
    /// <summary>
    /// Pings the hub.
    /// </summary>
    /// <returns>A pong.</returns>
    public Task<string> Ping()
    {
        return Task.FromResult("pong");
    }
}
