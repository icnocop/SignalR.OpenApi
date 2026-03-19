// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Request model for the SubmitFeedback method examples, containing the message and a note.
/// </summary>
public class SubmitFeedbackRequest
{
    /// <summary>
    /// Gets or sets the message to provide feedback on.
    /// </summary>
    public ChatMessage Message { get; set; } = new();

    /// <summary>
    /// Gets or sets an additional note or comment about the message.
    /// </summary>
    public string Note { get; set; } = string.Empty;
}
