// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.PrimitiveParameters;

/// <summary>
/// A hub whose methods take only primitive parameters.
/// </summary>
public class PrimitiveParameterHub : Hub
{
    /// <summary>
    /// Sends a message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>An echo of the message.</returns>
    public Task<string> SendMessage(string message)
    {
        return Task.FromResult(message);
    }

    /// <summary>
    /// Sends a message to a specific user.
    /// </summary>
    /// <param name="user">The recipient.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>An echo of the message.</returns>
    public Task<string> SendToUser(string user, string message)
    {
        return Task.FromResult($"{user}: {message}");
    }

    /// <summary>
    /// Gets the current time.
    /// </summary>
    /// <returns>The current UTC time.</returns>
    public Task<DateTime> GetTime()
    {
        return Task.FromResult(DateTime.UtcNow);
    }
}
