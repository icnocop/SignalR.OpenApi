// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// Client interface where all events have custom <see cref="TagsAttribute"/>.
/// Used to verify the default "{HubName} Events" tag is omitted.
/// </summary>
public interface IAllTaggedClient
{
    /// <summary>
    /// Notifies that a user connected.
    /// </summary>
    /// <param name="user">The user who connected.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Tags("Notifications")]
    Task UserConnected(string user);

    /// <summary>
    /// Notifies that a user disconnected.
    /// </summary>
    /// <param name="user">The user who disconnected.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Tags("Notifications")]
    Task UserDisconnected(string user);
}
