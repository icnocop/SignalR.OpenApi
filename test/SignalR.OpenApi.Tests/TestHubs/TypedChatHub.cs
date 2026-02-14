// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A typed hub with a client interface.
/// </summary>
public class TypedChatHub : Hub<IChatClient>
{
    /// <summary>
    /// Sends a message to all clients.
    /// </summary>
    /// <param name="user">The sender.</param>
    /// <param name="message">The message content.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SendMessage(string user, string message)
    {
        await this.Clients.All.ReceiveMessage(user, message);
    }
}
