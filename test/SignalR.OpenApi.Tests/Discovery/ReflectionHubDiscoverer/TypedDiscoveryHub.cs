// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Discovery;

/// <summary>
/// A strongly typed hub used to verify client interface and client event discovery.
/// </summary>
public class TypedDiscoveryHub : Hub<ITypedDiscoveryClient>
{
    /// <summary>
    /// Sends a message to all clients.
    /// </summary>
    /// <param name="user">The sender.</param>
    /// <param name="message">The message content.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Broadcast(string user, string message)
    {
        await this.Clients.All.ReceiveMessage(user, message);
    }
}
