// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// Provides example shapes for testing polymorphic example filtering.
/// </summary>
public class ShapeExamplesProvider : ISignalROpenApiExamplesProvider<TestShape>
{
    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<TestShape>> GetExamples()
    {
        yield return new SignalROpenApiExample<TestShape>(
            "SmallCircle",
            new CircleShape { Color = "red", Radius = 5.0 })
        {
            Summary = "A small red circle",
        };

        yield return new SignalROpenApiExample<TestShape>(
            "LargeRectangle",
            new RectangleShape { Color = "blue", Width = 100.0, Height = 50.0 })
        {
            Summary = "A large blue rectangle",
        };
    }
}
