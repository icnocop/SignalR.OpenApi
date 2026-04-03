// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Represents chat room statistics returned by the hub.
/// </summary>
public class ChatStats
{
    /// <summary>
    /// Gets or sets the number of currently active users.
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// Gets or sets the total number of messages sent.
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Gets or sets the name of the chat room.
    /// </summary>
    public string RoomName { get; set; } = string.Empty;
}
