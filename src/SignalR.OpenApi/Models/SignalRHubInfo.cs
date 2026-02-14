// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Reflection;

namespace SignalR.OpenApi.Models;

/// <summary>
/// Represents metadata about a discovered SignalR hub.
/// </summary>
public sealed class SignalRHubInfo
{
    /// <summary>
    /// Gets or sets the hub type.
    /// </summary>
    public required Type HubType { get; set; }

    /// <summary>
    /// Gets or sets the hub name (derived from type name or configuration).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the hub route path (e.g., "/hubs/chat").
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the summary text from <c>[EndpointSummary]</c> or XML docs.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the description text from <c>[EndpointDescription]</c> or XML docs.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the tags for grouping in the OpenAPI document.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the hub requires authorization.
    /// </summary>
    public bool RequiresAuthorization { get; set; }

    /// <summary>
    /// Gets or sets the authorization policy names on the hub.
    /// </summary>
    public IReadOnlyList<string> AuthorizationPolicies { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the hub is deprecated.
    /// </summary>
    public bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets or sets the server-to-client methods (hub methods callable by clients).
    /// </summary>
    public IReadOnlyList<SignalRMethodInfo> Methods { get; set; } = [];

    /// <summary>
    /// Gets or sets the client events (methods on <c>Hub&lt;TClient&gt;</c> interface).
    /// </summary>
    public IReadOnlyList<SignalRClientEventInfo> ClientEvents { get; set; } = [];

    /// <summary>
    /// Gets or sets the client interface type for typed hubs (<c>Hub&lt;TClient&gt;</c>).
    /// </summary>
    public Type? ClientInterfaceType { get; set; }
}
