// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// Provides request examples for the CreateOrder method.
/// </summary>
public class OrderRequestExamplesProvider : ISignalROpenApiExamplesProvider<OrderRequest>
{
    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<OrderRequest>> GetExamples()
    {
        yield return new SignalROpenApiExample<OrderRequest>(
            "SingleItem",
            new OrderRequest { Product = "Widget", Quantity = 1, Email = "alice@example.com" })
        {
            Summary = "Single item order",
            Description = "A basic order for one widget.",
        };

        yield return new SignalROpenApiExample<OrderRequest>(
            "BulkOrder",
            new OrderRequest { Product = "Gadget", Quantity = 100, Email = "purchasing@example.com" })
        {
            Summary = "Bulk order",
            Description = "A large order for 100 gadgets.",
        };
    }
}
