// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Provides request examples for the ReplyToMessage method.
/// </summary>
public class ReplyToMessageExamplesProvider : ISignalROpenApiExamplesProvider<ReplyToMessageRequest>
{
    private readonly IDateTimeProvider dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplyToMessageExamplesProvider"/> class.
    /// </summary>
    /// <param name="dateTimeProvider">The date and time provider.</param>
    public ReplyToMessageExamplesProvider(IDateTimeProvider dateTimeProvider)
    {
        this.dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<ReplyToMessageRequest>> GetExamples()
    {
        var now = this.dateTimeProvider.Now();

        yield return new SignalROpenApiExample<ReplyToMessageRequest>(
            "ReplyToGreeting",
            new ReplyToMessageRequest
            {
                OriginalMessage = new ChatMessage
                {
                    User = "Alice",
                    Message = "Hello, everyone!",
                    Timestamp = now,
                },
                Reply = new ChatMessage
                {
                    User = "Bob",
                    Message = "Hi Alice, welcome!",
                    Timestamp = now,
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
                    Timestamp = now,
                },
                Reply = new ChatMessage
                {
                    User = "Alice",
                    Message = "It starts at 3 PM.",
                    Timestamp = now,
                },
            })
        {
            Summary = "Answering a question",
        };
    }
}
