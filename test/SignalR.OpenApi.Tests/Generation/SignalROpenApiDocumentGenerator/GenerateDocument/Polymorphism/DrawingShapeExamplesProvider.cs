// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Polymorphism;

/// <summary>
/// Provides one example per derived shape, so sub-endpoint example filtering can be verified.
/// </summary>
public class DrawingShapeExamplesProvider : ISignalROpenApiExamplesProvider<DrawingShape>
{
    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<DrawingShape>> GetExamples()
    {
        yield return new SignalROpenApiExample<DrawingShape>(
            "SmallCircle",
            new CircleDrawing { Color = "red", Radius = 5.0 })
        {
            Summary = "A small red circle",
        };

        yield return new SignalROpenApiExample<DrawingShape>(
            "LargeRectangle",
            new RectangleDrawing { Color = "blue", Width = 100.0, Height = 50.0 })
        {
            Summary = "A large blue rectangle",
        };
    }
}
