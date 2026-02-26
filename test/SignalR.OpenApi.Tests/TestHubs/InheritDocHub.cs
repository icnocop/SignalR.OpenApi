// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <inheritdoc cref="IInheritDocHub"/>
public class InheritDocHub : Hub, IInheritDocHub
{
    /// <inheritdoc />
    public Task<string> Greet(string name)
    {
        return Task.FromResult($"Hello, {name}!");
    }
}
