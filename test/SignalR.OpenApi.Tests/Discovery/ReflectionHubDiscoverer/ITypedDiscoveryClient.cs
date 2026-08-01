// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace SignalR.OpenApi.Tests.Discovery;

/// <summary>
/// A client interface used to verify client event discovery and event-level tags.
/// </summary>
public interface ITypedDiscoveryClient
{
    /// <summary>
    /// Receives a chat message.
    /// </summary>
    /// <param name="user">The user who sent the message.</param>
    /// <param name="message">The message content.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ReceiveMessage(string user, string message);

    /// <summary>
    /// Notifies that a user joined.
    /// </summary>
    /// <param name="user">The user who joined.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Tags("Presence")]
    Task UserJoined(string user);
}
