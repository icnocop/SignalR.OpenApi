// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.SwaggerUi.Tests;

/// <summary>
/// The request model for the UI example operation.
/// </summary>
public class UiOrderRequest
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Product { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }
}
