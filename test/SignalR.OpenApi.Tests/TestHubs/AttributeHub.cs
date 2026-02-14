// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A hub demonstrating all supported attributes.
/// </summary>
[Authorize]
[Tags("Admin")]
public class AttributeHub : Hub
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
    /// A hidden method.
    /// </summary>
    /// <returns>Hidden value.</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<string> HiddenMethod()
    {
        return Task.FromResult("hidden");
    }

    /// <summary>
    /// Method with validated input.
    /// </summary>
    /// <param name="input">The input model.</param>
    /// <returns>Validation result.</returns>
    public Task<string> ValidateInput(ValidatedModel input)
    {
        return Task.FromResult("valid");
    }

    /// <summary>
    /// Method with two complex object parameters (not flat).
    /// </summary>
    /// <param name="first">The first model.</param>
    /// <param name="second">The second model.</param>
    /// <returns>Combined result.</returns>
    public Task<string> MultiObjectInput(ValidatedModel first, ValidatedModel second)
    {
        return Task.FromResult($"{first.Name} and {second.Name}");
    }
}
