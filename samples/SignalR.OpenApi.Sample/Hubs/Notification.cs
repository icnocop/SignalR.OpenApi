// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Base type for polymorphic notifications.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextNotification), "text")]
[JsonDerivedType(typeof(AlertNotification), "alert")]
public abstract class Notification
{
    /// <summary>
    /// Gets or sets the recipient of the notification.
    /// </summary>
    public required string Recipient { get; set; }
}
