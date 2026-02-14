// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;
using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A hub demonstrating example provider attributes.
/// </summary>
public class ExampleHub : Hub
{
    /// <summary>
    /// Creates an order.
    /// </summary>
    /// <param name="order">The order to create.</param>
    /// <returns>The order identifier.</returns>
    [SignalROpenApiRequestExamples(typeof(OrderRequestExamplesProvider))]
    [SignalROpenApiResponseExamples(typeof(OrderResponseExamplesProvider))]
    public Task<OrderResult> CreateOrder(OrderRequest order)
    {
        return Task.FromResult(new OrderResult { OrderId = "ORD-001", Status = "Created" });
    }

    /// <summary>
    /// Gets the greeting.
    /// </summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A greeting message.</returns>
    [SignalROpenApiResponseExamples(typeof(GreetingExamplesProvider))]
    public Task<string> Greet(string name)
    {
        return Task.FromResult($"Hello, {name}!");
    }
}
