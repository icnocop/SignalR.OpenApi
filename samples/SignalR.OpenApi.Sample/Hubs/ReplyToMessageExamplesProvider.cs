// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Provides request examples for the ReplyToMessage method.
/// </summary>
public class ReplyToMessageExamplesProvider : ISignalROpenApiExamplesProvider<ReplyToMessageRequest>
{
    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<ReplyToMessageRequest>> GetExamples()
    {
        yield return new SignalROpenApiExample<ReplyToMessageRequest>(
            "ReplyToGreeting",
            new ReplyToMessageRequest
            {
                OriginalMessage = new ChatMessage
                {
                    User = "Alice",
                    Message = "Hello, everyone!",
                    Timestamp = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero),
                },
                Reply = new ChatMessage
                {
                    User = "Bob",
                    Message = "Hi Alice, welcome!",
                    Timestamp = new DateTimeOffset(2026, 2, 15, 10, 1, 0, TimeSpan.Zero),
                },
            })
        {
            Summary = "Replying to a greeting",
        };

        yield return new SignalROpenApiExample<ReplyToMessageRequest>(
            "AnswerQuestion",
            new ReplyToMessageRequest
            {
                OriginalMessage = new ChatMessage
                {
                    User = "Bob",
                    Message = "What time is the meeting?",
                    Timestamp = new DateTimeOffset(2026, 2, 15, 14, 0, 0, TimeSpan.Zero),
                },
                Reply = new ChatMessage
                {
                    User = "Alice",
                    Message = "It starts at 3 PM.",
                    Timestamp = new DateTimeOffset(2026, 2, 15, 14, 2, 0, TimeSpan.Zero),
                },
            })
        {
            Summary = "Answering a question",
        };
    }
}
