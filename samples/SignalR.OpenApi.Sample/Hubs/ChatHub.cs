// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// A sample chat hub demonstrating SignalR.OpenApi features.
/// </summary>
[Tags("Chat")]
public class ChatHub : Hub<IChatClient>
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
    [SignalROpenApiRequestExamples(typeof(SendMessageExamplesProvider))]
    public async Task SendMessage(string user, string message)
    {
        await this.Clients.All.ReceiveMessage(user, message);
    }

    /// <summary>
    /// Sends a direct message using a request object with FluentValidation.
    /// </summary>
    /// <remarks>
    /// This demonstrates hub methods with a single object parameter (flattened schema).
    /// FluentValidation rules from <see cref="SendMessageRequestValidator"/> are applied to the OpenAPI schema.
    /// </remarks>
    /// <param name="request">The message request containing user and message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [SignalROpenApiRequestExamples(typeof(SendMessageExamplesProvider))]
    public async Task SendDirectMessage(SendMessageRequest request)
    {
        await this.Clients.All.ReceiveMessage(request.User, request.Message);
    }

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
    [SignalROpenApiRequestExamples(typeof(ReplyToMessageExamplesProvider))]
    public async Task ReplyToMessage(ChatMessage originalMessage, ChatMessage reply)
    {
        await this.Clients.All.ReceiveMessage(reply.User, $"Re: {originalMessage.Message} — {reply.Message}");
    }

    /// <summary>
    /// Sends a message to a specific group.
    /// </summary>
    /// <param name="group">The target group name.</param>
    /// <param name="user">The name of the sending user.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SendToGroup(string group, string user, string message)
    {
        await this.Clients.Group(group).ReceiveMessage(user, message);
    }

    /// <summary>
    /// Sends a notification to a user. The notification type is polymorphic —
    /// use the "type" discriminator to select between "text" and "alert".
    /// </summary>
    /// <param name="notification">The notification to send (text or alert).</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [SignalROpenApiRequestExamples(typeof(NotificationExamplesProvider))]
    public async Task SendNotification(Notification notification)
    {
        await this.Clients.All.ReceiveMessage(notification.Recipient, $"Notification: {notification.GetType().Name}");
    }

    /// <summary>
    /// Streams a countdown of numbers.
    /// </summary>
    /// <param name="from">The starting number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream of countdown numbers.</returns>
    /// <example>10.</example>
    public async IAsyncEnumerable<int> Countdown(
        int from,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = from; i >= 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
            await Task.Delay(1000, cancellationToken);
        }
    }
}
