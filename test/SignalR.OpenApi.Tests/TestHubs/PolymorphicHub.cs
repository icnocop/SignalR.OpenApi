// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A hub with a polymorphic parameter for testing.
/// </summary>
public class PolymorphicHub : Hub
{
    /// <summary>
    /// Draws a shape.
    /// </summary>
    /// <param name="shape">The shape to draw.</param>
    /// <returns>A description of the drawn shape.</returns>
    [SignalR.OpenApi.Examples.SignalROpenApiRequestExamples(typeof(ShapeExamplesProvider))]
    public Task<string> DrawShape(TestShape shape)
    {
        return Task.FromResult($"Drew {shape.GetType().Name}");
    }
}
