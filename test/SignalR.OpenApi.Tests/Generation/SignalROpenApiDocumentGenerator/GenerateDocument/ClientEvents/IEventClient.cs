// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.Http;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.ClientEvents;

/// <summary>
/// A client interface covering a plain event, a custom-tagged event, and a
/// polymorphic-payload event.
/// </summary>
public interface IEventClient
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

    /// <summary>
    /// Receives a polymorphic notification.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task NotificationRaised(NotificationShape notification);
}
