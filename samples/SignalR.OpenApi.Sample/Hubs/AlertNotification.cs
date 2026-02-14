// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// An alert notification with a severity level.
/// </summary>
public class AlertNotification : Notification
{
    /// <summary>
    /// Gets or sets the alert title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets the severity level (e.g., "info", "warning", "critical").
    /// </summary>
    public required string Severity { get; set; }
}
