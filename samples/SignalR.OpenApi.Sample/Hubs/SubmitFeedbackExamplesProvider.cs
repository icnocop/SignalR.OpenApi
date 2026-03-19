// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Provides request examples for the SubmitFeedback method.
/// </summary>
public class SubmitFeedbackExamplesProvider : ISignalROpenApiExamplesProvider<SubmitFeedbackRequest>
{
    private readonly IDateTimeProvider dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitFeedbackExamplesProvider"/> class.
    /// </summary>
    /// <param name="dateTimeProvider">The date and time provider.</param>
    public SubmitFeedbackExamplesProvider(IDateTimeProvider dateTimeProvider)
    {
        this.dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<SubmitFeedbackRequest>> GetExamples()
    {
        var now = this.dateTimeProvider.Now();

        yield return new SignalROpenApiExample<SubmitFeedbackRequest>(
            "PositiveFeedback",
            new SubmitFeedbackRequest
            {
                Message = new ChatMessage
                {
                    User = "Alice",
                    Message = "Check out the new feature!",
                    Timestamp = now,
                },
                Note = "Great work on this feature!",
            })
        {
            Summary = "Positive feedback on a message",
        };

        yield return new SignalROpenApiExample<SubmitFeedbackRequest>(
            "FollowUpNote",
            new SubmitFeedbackRequest
            {
                Message = new ChatMessage
                {
                    User = "Bob",
                    Message = "The deployment is scheduled for Friday.",
                    Timestamp = now,
                },
                Note = "Please confirm the rollback plan is ready.",
            })
        {
            Summary = "Follow-up note on a deployment message",
        };
    }
}
