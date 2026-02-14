// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Represents a chat message with sender, content, and timestamp.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Gets or sets the user who sent the message.
    /// </summary>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message was sent.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
