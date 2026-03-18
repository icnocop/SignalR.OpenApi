// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Provides the current date and time.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    /// <returns>The current local date and time as a <see cref="DateTimeOffset"/>.</returns>
    DateTimeOffset Now();
}
