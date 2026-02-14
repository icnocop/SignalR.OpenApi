// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A test order request model.
/// </summary>
public class OrderRequest
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Product { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the customer email.
    /// </summary>
    public string? Email { get; set; }
}
