// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Request model for the ReplyToMessage method examples, containing the original message and the reply.
/// </summary>
public class ReplyToMessageRequest
{
    /// <summary>
    /// Gets or sets the original message being replied to.
    /// </summary>
    public ChatMessage OriginalMessage { get; set; } = new();

    /// <summary>
    /// Gets or sets the reply message.
    /// </summary>
    public ChatMessage Reply { get; set; } = new();
}
