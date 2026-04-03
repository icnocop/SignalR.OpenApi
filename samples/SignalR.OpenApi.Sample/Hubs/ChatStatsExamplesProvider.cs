// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Provides response examples for the GetChatStats method.
/// </summary>
public class ChatStatsExamplesProvider : ISignalROpenApiExamplesProvider<ChatStats>
{
    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<ChatStats>> GetExamples()
    {
        yield return new SignalROpenApiExample<ChatStats>(
            "Busy",
            new ChatStats { ActiveUsers = 42, TotalMessages = 1583, RoomName = "General" })
        {
            Summary = "A busy chat room",
        };

        yield return new SignalROpenApiExample<ChatStats>(
            "Quiet",
            new ChatStats { ActiveUsers = 3, TotalMessages = 27, RoomName = "Watercooler" })
        {
            Summary = "A quiet chat room",
        };
    }
}
