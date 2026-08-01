// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Streaming;

/// <summary>
/// A hub with a streaming method.
/// </summary>
public class StreamingMethodHub : Hub
{
    /// <summary>
    /// Streams integers.
    /// </summary>
    /// <param name="count">The number of integers to stream.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A stream of integers.</returns>
    public async IAsyncEnumerable<int> StreamIntegers(
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
            await Task.Yield();
        }
    }

    /// <summary>
    /// Returns a single value, for contrast with the streaming method.
    /// </summary>
    /// <returns>A single value.</returns>
    public Task<int> GetSingleValue()
    {
        return Task.FromResult(42);
    }
}
