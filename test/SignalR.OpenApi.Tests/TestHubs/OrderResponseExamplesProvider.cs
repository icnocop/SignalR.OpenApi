// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// Provides response examples for the CreateOrder method.
/// </summary>
public class OrderResponseExamplesProvider : ISignalROpenApiExamplesProvider<OrderResult>
{
    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<OrderResult>> GetExamples()
    {
        yield return new SignalROpenApiExample<OrderResult>(
            "Created",
            new OrderResult { OrderId = "ORD-001", Status = "Created" })
        {
            Summary = "Successfully created order",
        };

        yield return new SignalROpenApiExample<OrderResult>(
            "Pending",
            new OrderResult { OrderId = "ORD-002", Status = "PendingApproval" })
        {
            Summary = "Order pending approval",
        };
    }
}
