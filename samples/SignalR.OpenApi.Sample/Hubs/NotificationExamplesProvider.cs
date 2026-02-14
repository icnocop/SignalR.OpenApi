// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Provides request examples for the SendNotification method.
/// </summary>
public class NotificationExamplesProvider : ISignalROpenApiExamplesProvider<Notification>
{
    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<Notification>> GetExamples()
    {
        yield return new SignalROpenApiExample<Notification>(
            "TextNotification",
            new TextNotification { Recipient = "Alice", Content = "Hello, you have a new message!" })
        {
            Summary = "A text notification",
        };

        yield return new SignalROpenApiExample<Notification>(
            "AlertNotification",
            new AlertNotification { Recipient = "Bob", Title = "Server Alert", Severity = "critical" })
        {
            Summary = "An alert notification",
        };
    }
}
