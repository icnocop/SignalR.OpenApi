// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Reflection;

namespace SignalR.OpenApi.Models;

/// <summary>
/// Represents metadata about a SignalR hub method.
/// </summary>
public sealed class SignalRMethodInfo
{
    /// <summary>
    /// Gets or sets the underlying <see cref="System.Reflection.MethodInfo"/>.
    /// </summary>
    public required MethodInfo MethodInfo { get; set; }

    /// <summary>
    /// Gets or sets the method name as it appears in the SignalR protocol.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the operation ID for the OpenAPI document.
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    /// Gets or sets the summary text.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the description text.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the tags for grouping.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// Gets or sets the method parameters.
    /// </summary>
    public IReadOnlyList<SignalRParameterInfo> Parameters { get; set; } = [];

    /// <summary>
    /// Gets or sets the return type (unwrapped from <c>Task&lt;T&gt;</c>).
    /// </summary>
    public Type? ReturnType { get; set; }

    /// <summary>
    /// Gets or sets the return type description from XML docs.
    /// </summary>
    public string? ReturnDescription { get; set; }

    /// <summary>
    /// Gets or sets the response content types from <c>[Produces]</c>.
    /// </summary>
    public IReadOnlyList<string> ProducesContentTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this method returns a stream
    /// (<c>IAsyncEnumerable&lt;T&gt;</c> or <c>ChannelReader&lt;T&gt;</c>).
    /// </summary>
    public bool IsStreamingResponse { get; set; }

    /// <summary>
    /// Gets or sets the element type of the stream (the <c>T</c> in <c>IAsyncEnumerable&lt;T&gt;</c>).
    /// </summary>
    public Type? StreamItemType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this method requires authorization.
    /// </summary>
    public bool RequiresAuthorization { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this method allows anonymous access
    /// (overrides hub-level authorization).
    /// </summary>
    public bool AllowAnonymous { get; set; }

    /// <summary>
    /// Gets or sets the authorization policy names on the method.
    /// </summary>
    public IReadOnlyList<string> AuthorizationPolicies { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the method is deprecated.
    /// </summary>
    public bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets or sets the example value from XML docs.
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// Gets or sets the request example provider types from
    /// <see cref="Examples.SignalROpenApiRequestExamplesAttribute"/> attributes.
    /// </summary>
    public IReadOnlyList<Type> RequestExampleProviderTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the response example provider types from
    /// <see cref="Examples.SignalROpenApiResponseExamplesAttribute"/> attributes.
    /// </summary>
    public IReadOnlyList<Type> ResponseExampleProviderTypes { get; set; } = [];
}
