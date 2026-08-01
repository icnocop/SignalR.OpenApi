// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.SingleObjectParameter;

/// <summary>
/// A hub whose method takes a single flat complex object parameter.
/// </summary>
public class SingleObjectParameterHub : Hub
{
    /// <summary>
    /// Saves a profile.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    /// <returns>A confirmation message.</returns>
    public Task<string> SaveProfile(AnnotatedProfile profile)
    {
        return Task.FromResult(profile.Name);
    }
}
