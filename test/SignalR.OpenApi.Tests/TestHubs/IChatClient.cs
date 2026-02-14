// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// Client interface for the typed hub.
/// </summary>
public interface IChatClient
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
    Task UserJoined(string user);
}
