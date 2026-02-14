// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A hub with streaming methods.
/// </summary>
public class StreamingHub : Hub
{
    /// <summary>
    /// Streams a sequence of integers.
    /// </summary>
    /// <param name="count">Number of items to stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream of integers.</returns>
    public async IAsyncEnumerable<int> StreamIntegers(
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return i;
            await Task.Delay(100, cancellationToken);
        }
    }

    /// <summary>
    /// Streams via a channel reader.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A channel reader of strings.</returns>
    public ChannelReader<string> StreamViaChannel(CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<string>();
        return channel.Reader;
    }
}
