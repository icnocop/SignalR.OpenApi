// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Deprecation;

/// <summary>
/// A hub with an obsolete method.
/// </summary>
public class DeprecatedMethodHub : Hub
{
    /// <summary>
    /// A current method.
    /// </summary>
    /// <returns>The value.</returns>
    public Task<string> GetValue()
    {
        return Task.FromResult("value");
    }

    /// <summary>
    /// A deprecated method.
    /// </summary>
    /// <returns>Nothing useful.</returns>
    [Obsolete("Use GetValue instead.")]
    public Task<string> GetValueLegacy()
    {
        return Task.FromResult("deprecated");
    }
}
