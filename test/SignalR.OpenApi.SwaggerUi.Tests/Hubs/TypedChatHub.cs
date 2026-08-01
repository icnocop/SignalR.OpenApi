// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.SwaggerUi.Tests;

/// <summary>
/// A strongly typed hub, so the UI renders a client event operation.
/// </summary>
public class TypedChatHub : Hub<IChatClient>
{
    /// <summary>
    /// Broadcasts a message to all clients.
    /// </summary>
    /// <param name="user">The sender.</param>
    /// <param name="message">The message content.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Broadcast(string user, string message)
    {
        await this.Clients.All.ReceiveMessage(user, message);
    }
}
