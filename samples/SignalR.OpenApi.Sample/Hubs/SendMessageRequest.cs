// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Request model for SendMessage examples.
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message text.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
