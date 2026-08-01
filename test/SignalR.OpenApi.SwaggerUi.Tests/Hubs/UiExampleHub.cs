// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;
using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.SwaggerUi.Tests;

/// <summary>
/// A hub with response examples, so the UI renders the example dropdown the
/// Playwright tests look for.
/// </summary>
public class UiExampleHub : Hub
{
    /// <summary>
    /// Creates an order.
    /// </summary>
    /// <param name="order">The order to create.</param>
    /// <returns>The order result.</returns>
    [SignalROpenApiResponseExamples(typeof(UiOrderResponseExamplesProvider))]
    public Task<UiOrderResult> CreateOrder(UiOrderRequest order)
    {
        _ = order;
        return Task.FromResult(new UiOrderResult { OrderId = "ORD-001", Status = "Created" });
    }
}
