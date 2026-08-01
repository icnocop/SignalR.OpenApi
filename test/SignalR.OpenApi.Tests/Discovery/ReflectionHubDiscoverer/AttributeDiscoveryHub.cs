// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Discovery;

/// <summary>
/// A hub exercising every attribute discovery reads.
/// </summary>
[Authorize]
[Tags("Admin")]
public class AttributeDiscoveryHub : Hub
{
    /// <summary>
    /// Gets user details.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user's display name.</returns>
    [Tags("Users")]
    [EndpointName("GetUserDetails")]
    [EndpointSummary("Retrieves user details")]
    [EndpointDescription("Gets the display name for a given user ID.")]
    public Task<string> GetUserDetailsAsync([Description("The unique user ID")] string userId)
    {
        return Task.FromResult($"User {userId}");
    }

    /// <summary>
    /// A deprecated method.
    /// </summary>
    /// <returns>Nothing useful.</returns>
    [Obsolete("Use GetUserDetailsAsync instead.")]
    public Task<string> GetUserLegacy()
    {
        return Task.FromResult("deprecated");
    }

    /// <summary>
    /// An anonymous endpoint.
    /// </summary>
    /// <returns>The health status.</returns>
    [AllowAnonymous]
    public Task<string> HealthCheck()
    {
        return Task.FromResult("OK");
    }

    /// <summary>
    /// A method excluded from discovery.
    /// </summary>
    /// <returns>Hidden value.</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<string> HiddenMethod()
    {
        return Task.FromResult("hidden");
    }

    /// <summary>
    /// A method whose Async suffix must be preserved in the name and operation ID.
    /// </summary>
    /// <returns>The data.</returns>
    public Task<string> FetchDataAsync()
    {
        return Task.FromResult("data");
    }

    /// <summary>
    /// A method taking a model with data annotations.
    /// </summary>
    /// <param name="input">The input model.</param>
    /// <returns>Validation result.</returns>
    public Task<string> ValidateInput(DiscoveryValidatedModel input)
    {
        return Task.FromResult(input.Name);
    }
}
