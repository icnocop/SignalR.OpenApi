// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Discovery;

/// <summary>
/// A hub excluded from discovery by <see cref="ApiExplorerSettingsAttribute"/>.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class HiddenDiscoveryHub : Hub
{
    /// <summary>
    /// A method that should never be discovered.
    /// </summary>
    /// <returns>A value.</returns>
    public Task<string> ShouldNotAppear()
    {
        return Task.FromResult("hidden");
    }
}
