// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.ClientEvents;

/// <summary>
/// An alert <see cref="NotificationShape"/>.
/// </summary>
public class AlertNotificationShape : NotificationShape
{
    /// <summary>
    /// Gets or sets the severity.
    /// </summary>
    public int Severity { get; set; }
}
