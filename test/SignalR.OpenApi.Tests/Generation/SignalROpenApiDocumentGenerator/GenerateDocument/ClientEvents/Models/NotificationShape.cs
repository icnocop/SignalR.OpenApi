// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.ClientEvents;

/// <summary>
/// A polymorphic client event payload, used to verify eventDiscriminators metadata.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(TextNotificationShape), "text")]
[JsonDerivedType(typeof(AlertNotificationShape), "alert")]
public abstract class NotificationShape
{
    /// <summary>
    /// Gets or sets the recipient.
    /// </summary>
    public required string Recipient { get; set; }
}
