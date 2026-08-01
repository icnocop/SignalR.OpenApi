// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.SwaggerUi.Tests;

/// <summary>
/// A streaming hub, so the UI's stream controls can be exercised.
/// </summary>
public class StreamingHub : Hub
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
            await Task.Delay(50, cancellationToken);
        }
    }
}
