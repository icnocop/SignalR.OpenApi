// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.SwaggerUi.Tests;

/// <summary>
/// The result model for the UI example operation.
/// </summary>
public class UiOrderResult
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
