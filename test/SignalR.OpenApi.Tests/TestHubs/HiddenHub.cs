// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A hub that should be excluded from the spec.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class HiddenHub : Hub
{
    /// <summary>
    /// Hidden method.
    /// </summary>
    /// <returns>A hidden value.</returns>
    public Task<string> Secret()
    {
        return Task.FromResult("secret");
    }
}
