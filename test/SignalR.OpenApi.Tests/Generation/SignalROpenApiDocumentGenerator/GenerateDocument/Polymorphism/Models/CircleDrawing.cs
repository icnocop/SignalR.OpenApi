// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Polymorphism;

/// <summary>
/// A circular <see cref="DrawingShape"/>.
/// </summary>
public class CircleDrawing : DrawingShape
{
    /// <summary>
    /// Gets or sets the radius.
    /// </summary>
    public double Radius { get; set; }
}
