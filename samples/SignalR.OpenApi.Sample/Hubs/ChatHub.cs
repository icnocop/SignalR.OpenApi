// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Sample.Hubs;

/// <inheritdoc cref="IChatHub"/>
[Tags("Chat")]
public class ChatHub : Hub<IChatClient>, IChatHub
{
    /// <inheritdoc />
    [SignalROpenApiRequestExamples(typeof(SendMessageExamplesProvider))]
    public async Task SendMessageAsync(string user, string message)
    {
        await this.Clients.All.ReceiveMessage(user, message);
    }

    /// <inheritdoc />
    [SignalROpenApiRequestExamples(typeof(SendMessageExamplesProvider))]
    public async Task SendDirectMessageAsync(SendMessageRequest request)
    {
        await this.Clients.All.ReceiveMessage(request.User, request.Message);
    }

    /// <inheritdoc />
    [SignalROpenApiRequestExamples(typeof(ReplyToMessageExamplesProvider))]
    public async Task ReplyToMessageAsync(ChatMessage originalMessage, ChatMessage reply)
    {
        await this.Clients.All.ReceiveMessage(reply.User, $"Re: {originalMessage.Message} — {reply.Message}");
    }

    /// <inheritdoc />
    [SignalROpenApiRequestExamples(typeof(SubmitFeedbackExamplesProvider))]
    public async Task SubmitFeedbackAsync(ChatMessage message, string note)
    {
        await this.Clients.All.ReceiveMessage(message.User, $"[Feedback] {message.Message} — Note: {note}");
    }

    /// <inheritdoc />
    [Tags("Groups")]
    public async Task SendToGroupAsync(string group, string user, string message)
    {
        await this.Clients.Group(group).ReceiveMessage(user, message);
    }

    /// <inheritdoc />
    [Tags("Notifications")]
    [SignalROpenApiRequestExamples(typeof(NotificationExamplesProvider))]
    public async Task SendNotificationAsync(Notification notification)
    {
        var message = notification switch
        {
            TextNotification text => text.Content,
            AlertNotification alert => $"[{alert.Severity}] {alert.Title}",
            _ => $"Notification: {notification.GetType().Name}",
        };

        await this.Clients.All.ReceiveMessage(notification.Recipient, message);
        await this.Clients.All.ReceiveNotification(notification);
    }

    /// <inheritdoc />
    [Tags("Streaming")]
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
