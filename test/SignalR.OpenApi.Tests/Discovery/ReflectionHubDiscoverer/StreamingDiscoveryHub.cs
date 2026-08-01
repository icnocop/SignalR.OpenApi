// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Discovery;

/// <summary>
/// A hub covering both streaming shapes, plus a <see cref="CancellationToken"/> parameter
/// that discovery is expected to filter out.
/// </summary>
public class StreamingDiscoveryHub : Hub
{
    /// <summary>
    /// Streams integers via <see cref="IAsyncEnumerable{T}"/>.
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
    /// Streams strings via a <see cref="ChannelReader{T}"/>.
    /// </summary>
    /// <returns>A stream of strings.</returns>
    public ChannelReader<string> StreamViaChannel()
    {
        var channel = Channel.CreateUnbounded<string>();
        channel.Writer.Complete();
        return channel.Reader;
    }
}
