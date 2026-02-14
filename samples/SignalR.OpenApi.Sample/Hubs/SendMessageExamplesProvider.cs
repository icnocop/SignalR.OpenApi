// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Provides request examples for the SendMessage method.
/// </summary>
public class SendMessageExamplesProvider : ISignalROpenApiExamplesProvider<SendMessageRequest>
{
    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<SendMessageRequest>> GetExamples()
    {
        yield return new SignalROpenApiExample<SendMessageRequest>(
            "Greeting",
            new SendMessageRequest { User = "Alice", Message = "Hello, everyone!" })
        {
            Summary = "A friendly greeting",
        };

        yield return new SignalROpenApiExample<SendMessageRequest>(
            "Question",
            new SendMessageRequest { User = "Bob", Message = "What time is the meeting?" })
        {
            Summary = "Asking a question",
        };
    }
}
