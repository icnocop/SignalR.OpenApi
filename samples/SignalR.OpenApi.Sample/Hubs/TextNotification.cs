// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// A text notification sent to a user.
/// </summary>
public class TextNotification : Notification
{
    /// <summary>
    /// Gets or sets the text content of the notification.
    /// </summary>
    public required string Content { get; set; }
}
