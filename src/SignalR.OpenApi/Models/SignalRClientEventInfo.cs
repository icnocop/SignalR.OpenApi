// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Models;

/// <summary>
/// Represents a client event (server-to-client callback) on a typed hub's client interface.
/// </summary>
public sealed class SignalRClientEventInfo
{
    /// <summary>
    /// Gets or sets the event name as defined on the client interface.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the summary from XML docs.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the description from XML docs.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the parameter types of the client event.
    /// </summary>
    public IReadOnlyList<SignalRParameterInfo> Parameters { get; set; } = [];
}
