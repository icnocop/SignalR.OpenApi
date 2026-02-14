// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Examples;

/// <summary>
/// Represents a named example value for OpenAPI documentation.
/// </summary>
/// <typeparam name="T">The type of the example value.</typeparam>
public sealed class SignalROpenApiExample<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignalROpenApiExample{T}"/> class.
    /// </summary>
    /// <param name="name">The unique name of the example.</param>
    /// <param name="value">The example value.</param>
    public SignalROpenApiExample(string name, T value)
    {
        this.Name = name;
        this.Value = value;
    }

    /// <summary>
    /// Gets or sets the unique name of the example (used as the key in the OpenAPI <c>examples</c> map).
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a short summary of the example.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets a long description of the example.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the example value.
    /// </summary>
    public T Value { get; set; }
}
