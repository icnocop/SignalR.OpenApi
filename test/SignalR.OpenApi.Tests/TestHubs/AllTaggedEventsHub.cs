// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A typed hub whose client events all have custom <see cref="Microsoft.AspNetCore.Http.TagsAttribute"/>.
/// </summary>
public class AllTaggedEventsHub : Hub<IAllTaggedClient>
{
    /// <summary>
    /// Sends a ping to all clients.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Ping()
    {
        await this.Clients.All.UserConnected("system");
    }
}
