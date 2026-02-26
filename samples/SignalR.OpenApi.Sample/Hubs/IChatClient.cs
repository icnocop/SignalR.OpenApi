// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Client interface for the chat hub.
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// Receives a chat message from a user.
    /// </summary>
    /// <param name="user">The user who sent the message.</param>
    /// <param name="message">The message content.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ReceiveMessage(string user, string message);

    /// <summary>
    /// Notifies that a user has connected.
    /// </summary>
    /// <param name="user">The user who connected.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UserConnected(string user);

    /// <summary>
    /// Receives a notification object. The notification type is polymorphic —
    /// the "type" discriminator indicates "text" or "alert".
    /// </summary>
    /// <param name="notification">The notification that was received.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ReceiveNotification(Notification notification);
}
