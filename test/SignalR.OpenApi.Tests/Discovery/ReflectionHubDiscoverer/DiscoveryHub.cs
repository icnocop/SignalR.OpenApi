// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Discovery;

/// <summary>
/// A plain hub used to verify basic discovery, return-type unwrapping, and that inherited
/// <see cref="Hub"/> members are not surfaced as hub methods.
/// </summary>
public class DiscoveryHub : Hub
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
    /// Gets the current time.
    /// </summary>
    /// <returns>The current UTC time.</returns>
    public Task<DateTime> GetTime()
    {
        return Task.FromResult(DateTime.UtcNow);
    }
}
