// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Polymorphism;

/// <summary>
/// An abstract polymorphic base with a string discriminator.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(CircleDrawing), "circle")]
[JsonDerivedType(typeof(RectangleDrawing), "rectangle")]
public abstract class DrawingShape
{
    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    public required string Color { get; set; }
}
