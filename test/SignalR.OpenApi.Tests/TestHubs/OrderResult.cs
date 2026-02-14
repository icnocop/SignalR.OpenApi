// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A test order result model.
/// </summary>
public class OrderResult
{
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
