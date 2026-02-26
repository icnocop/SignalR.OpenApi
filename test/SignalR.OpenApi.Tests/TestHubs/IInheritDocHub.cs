// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// Defines the server-side methods for the inherited-doc hub.
/// </summary>
public interface IInheritDocHub
{
    /// <summary>
    /// Greets a user by name.
    /// </summary>
    /// <param name="name">The user's name.</param>
    /// <returns>A greeting message.</returns>
    Task<string> Greet(string name);
}
