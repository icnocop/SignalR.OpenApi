// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Tags;

/// <summary>
/// A documented hub for tag testing.
/// </summary>
public class DocumentedTagHub : Hub
{
    /// <summary>
    /// A method that uses the default hub tag.
    /// </summary>
    /// <returns>A value.</returns>
    public Task<string> DefaultTagged()
    {
        return Task.FromResult("value");
    }

    /// <summary>
    /// A method with a custom tag.
    /// </summary>
    /// <returns>A value.</returns>
    [Tags("Users")]
    public Task<string> CustomTagged()
    {
        return Task.FromResult("value");
    }

    /// <summary>
    /// Another method with the same custom tag, so deduplication can be observed.
    /// </summary>
    /// <returns>A value.</returns>
    [Tags("Users")]
    public Task<string> AlsoCustomTagged()
    {
        return Task.FromResult("value");
    }
}
