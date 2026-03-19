// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Defines the server-side methods for the chat hub.
/// </summary>
public interface IChatHub
{
    /// <summary>
    /// Sends a message to all connected clients using separate parameters.
    /// </summary>
    /// <remarks>
    /// This demonstrates hub methods with primitive parameters.
    /// FluentValidation does not apply to primitive parameters — use a request object instead.
    /// </remarks>
    /// <param name="user">The name of the sending user.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SendMessageAsync(string user, string message);

    /// <summary>
    /// Sends a direct message using a request object with FluentValidation.
    /// </summary>
    /// <remarks>
    /// This demonstrates hub methods with a single object parameter (flattened schema).
    /// FluentValidation rules from <see cref="SendMessageRequestValidator"/> are applied to the OpenAPI schema.
    /// </remarks>
    /// <param name="request">The message request containing user and message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SendDirectMessageAsync(SendMessageRequest request);

    /// <summary>
    /// Replies to an existing message.
    /// </summary>
    /// <remarks>
    /// This demonstrates hub methods with two object parameters (wrapped schema).
    /// Each parameter appears as a named property in the request body.
    /// </remarks>
    /// <param name="originalMessage">The original message being replied to.</param>
    /// <param name="reply">The reply message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ReplyToMessageAsync(ChatMessage originalMessage, ChatMessage reply);

    /// <summary>
    /// Sends a message to a specific group.
    /// </summary>
    /// <param name="group">The target group name.</param>
    /// <param name="user">The name of the sending user.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SendToGroupAsync(string group, string user, string message);

    /// <summary>
    /// Submits feedback for a message, combining the original message with a note.
    /// </summary>
    /// <remarks>
    /// This demonstrates hub methods with a complex object and a primitive string parameter.
    /// The request body contains both parameters as named properties (JSON-only, no form encoding).
    /// </remarks>
    /// <param name="message">The message to provide feedback on.</param>
    /// <param name="note">An additional note or comment about the message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SubmitFeedbackAsync(ChatMessage message, string note);

    /// <summary>
    /// Sends a notification to a user. The notification type is polymorphic —
    /// use the "type" discriminator to select between "text" and "alert".
    /// </summary>
    /// <param name="notification">The notification to send (text or alert).</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SendNotificationAsync(Notification notification);

    /// <summary>
    /// Streams a countdown of numbers.
    /// </summary>
    /// <param name="from">The starting number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream of countdown numbers.</returns>
    /// <example>10.</example>
    IAsyncEnumerable<int> Countdown(int from, CancellationToken cancellationToken);
}
