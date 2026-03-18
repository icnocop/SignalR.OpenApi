// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Provides the current local date and time.
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc/>
    public DateTimeOffset Now() => DateTimeOffset.Now;
}
