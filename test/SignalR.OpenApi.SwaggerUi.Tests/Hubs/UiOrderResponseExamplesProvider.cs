// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.SwaggerUi.Tests;

/// <summary>
/// Supplies two named response examples, so Swagger UI renders an example dropdown.
/// The names and values are asserted on by the Playwright tests.
/// </summary>
public class UiOrderResponseExamplesProvider : ISignalROpenApiExamplesProvider<UiOrderResult>
{
    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<UiOrderResult>> GetExamples()
    {
        yield return new SignalROpenApiExample<UiOrderResult>(
            "Created",
            new UiOrderResult { OrderId = "ORD-001", Status = "Created" })
        {
            Summary = "Successfully created order",
        };

        yield return new SignalROpenApiExample<UiOrderResult>(
            "Pending",
            new UiOrderResult { OrderId = "ORD-002", Status = "PendingApproval" })
        {
            Summary = "Order pending approval",
        };
    }
}
