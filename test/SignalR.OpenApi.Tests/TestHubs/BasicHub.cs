// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A basic hub for testing.
/// </summary>
public class BasicHub : Hub
{
    /// <summary>
    /// Sends a message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>An echo of the message.</returns>
    public Task<string> SendMessage(string message)
    {
        return Task.FromResult(message);
    }

    /// <summary>
    /// Gets the current time.
    /// </summary>
    /// <returns>The current UTC time.</returns>
    public Task<DateTime> GetTime()
    {
        return Task.FromResult(DateTime.UtcNow);
    }

    /// <summary>
    /// Submits feedback with an additional note.
    /// </summary>
    /// <param name="feedback">The feedback message.</param>
    /// <param name="note">An additional note for the feedback.</param>
    /// <returns>A confirmation message.</returns>
    public Task<string> SubmitFeedbackWithNote(FeedbackMessage feedback, string note)
    {
        return Task.FromResult($"{feedback.Sender}: {feedback.Content} — {note}");
    }
}
