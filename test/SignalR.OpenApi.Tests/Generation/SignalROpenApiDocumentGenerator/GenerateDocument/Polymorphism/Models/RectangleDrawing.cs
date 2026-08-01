// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Polymorphism;

/// <summary>
/// A rectangular <see cref="DrawingShape"/>.
/// </summary>
public class RectangleDrawing : DrawingShape
{
    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public double Height { get; set; }
}
