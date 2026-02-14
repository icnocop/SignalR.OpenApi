// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// Base type for polymorphic test shapes.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(CircleShape), "circle")]
[JsonDerivedType(typeof(RectangleShape), "rectangle")]
public abstract class TestShape
{
    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    public required string Color { get; set; }
}
