// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;
using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Polymorphism;

/// <summary>
/// A hub with a polymorphic parameter.
/// </summary>
public class PolymorphicParameterHub : Hub
{
    /// <summary>
    /// Draws a shape.
    /// </summary>
    /// <param name="shape">The shape to draw.</param>
    /// <returns>A description of the drawn shape.</returns>
    [SignalROpenApiRequestExamples(typeof(DrawingShapeExamplesProvider))]
    public Task<string> DrawShape(DrawingShape shape)
    {
        return Task.FromResult($"Drew {shape.GetType().Name}");
    }
}
