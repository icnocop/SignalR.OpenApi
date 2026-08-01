// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Responses;

/// <summary>
/// A hub with methods covering each return-type shape.
/// </summary>
public class ResponseHub : Hub
{
    /// <summary>
    /// Returns a value.
    /// </summary>
    /// <param name="message">The message to echo.</param>
    /// <returns>An echo of the message.</returns>
    public Task<string> Echo(string message)
    {
        return Task.FromResult(message);
    }

    /// <summary>
    /// Returns nothing.
    /// </summary>
    /// <param name="message">The message to record.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task Record(string message)
    {
        _ = message;
        return Task.CompletedTask;
    }
}
