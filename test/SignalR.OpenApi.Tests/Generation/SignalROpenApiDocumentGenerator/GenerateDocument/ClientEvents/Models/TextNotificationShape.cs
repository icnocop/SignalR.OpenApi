// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.ClientEvents;

/// <summary>
/// A textual <see cref="NotificationShape"/>.
/// </summary>
public class TextNotificationShape : NotificationShape
{
    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
