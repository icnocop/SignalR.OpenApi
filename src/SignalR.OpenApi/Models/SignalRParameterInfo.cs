// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Reflection;

namespace SignalR.OpenApi.Models;

/// <summary>
/// Represents metadata about a parameter of a SignalR hub method.
/// </summary>
public sealed class SignalRParameterInfo
{
    /// <summary>
    /// Gets or sets the underlying <see cref="System.Reflection.ParameterInfo"/>.
    /// </summary>
    public required ParameterInfo ParameterInfo { get; set; }

    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public required Type ParameterType { get; set; }

    /// <summary>
    /// Gets or sets the description from <c>[Description]</c> or XML <c>&lt;param&gt;</c> docs.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter has a default value.
    /// </summary>
    public bool HasDefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the default value if one exists.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this parameter is a streaming input
    /// (<c>IAsyncEnumerable&lt;T&gt;</c> or <c>ChannelReader&lt;T&gt;</c>).
    /// </summary>
    public bool IsStreamingInput { get; set; }

    /// <summary>
    /// Gets or sets the element type of a streaming input parameter.
    /// </summary>
    public Type? StreamItemType { get; set; }
}
