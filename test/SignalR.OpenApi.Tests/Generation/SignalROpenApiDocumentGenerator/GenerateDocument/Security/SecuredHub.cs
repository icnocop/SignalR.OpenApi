// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Security;

/// <summary>
/// An authorized hub. The presence of <see cref="AuthorizeAttribute"/> is what causes
/// configured security schemes to be emitted into the document.
/// </summary>
[Authorize]
public class SecuredHub : Hub
{
    /// <summary>
    /// Gets user details.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The user's display name.</returns>
    public Task<string> GetUserDetails(string userId)
    {
        return Task.FromResult($"User {userId}");
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
}
