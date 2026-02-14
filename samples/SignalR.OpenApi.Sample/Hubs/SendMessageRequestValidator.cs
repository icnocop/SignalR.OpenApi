// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using FluentValidation;

namespace SignalR.OpenApi.Sample.Hubs;

/// <summary>
/// Validates <see cref="SendMessageRequest"/> parameters.
/// </summary>
public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SendMessageRequestValidator"/> class.
    /// </summary>
    public SendMessageRequestValidator()
    {
        this.RuleFor(x => x.User)
            .NotEmpty()
            .Length(1, 50);

        this.RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(500);
    }
}
