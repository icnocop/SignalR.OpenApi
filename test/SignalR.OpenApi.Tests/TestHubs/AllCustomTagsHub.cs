// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A hub where every method has a custom <see cref="TagsAttribute"/>
/// that differs from the hub name. Used to verify the connection bar
/// still appears when no operation tag matches the hub name.
/// </summary>
public class AllCustomTagsHub : Hub
{
    /// <summary>
    /// Sends a greeting.
    /// </summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A greeting message.</returns>
    [Tags("Greetings")]
    public Task<string> SayHello(string name)
    {
        return Task.FromResult($"Hello, {name}!");
    }

    /// <summary>
    /// Sends a farewell.
    /// </summary>
    /// <param name="name">The name to bid farewell.</param>
    /// <returns>A farewell message.</returns>
    [Tags("Greetings")]
    public Task<string> SayGoodbye(string name)
    {
        return Task.FromResult($"Goodbye, {name}!");
    }
}
