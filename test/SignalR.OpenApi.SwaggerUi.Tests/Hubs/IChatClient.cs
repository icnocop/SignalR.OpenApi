// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.SwaggerUi.Tests;

/// <summary>
/// The client interface behind the event log panel the UI tests exercise.
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
}
