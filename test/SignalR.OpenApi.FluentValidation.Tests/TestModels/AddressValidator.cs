// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using FluentValidation;

namespace SignalR.OpenApi.FluentValidation.Tests.TestModels;

/// <summary>
/// Validator for <see cref="Address"/> demonstrating nested validator support.
/// </summary>
public class AddressValidator : AbstractValidator<Address>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddressValidator"/> class.
    /// </summary>
    public AddressValidator()
    {
        this.RuleFor(x => x.Street)
            .NotEmpty()
            .MaximumLength(200);

        this.RuleFor(x => x.City)
            .NotEmpty();

        this.RuleFor(x => x.ZipCode)
            .NotEmpty()
            .Matches(@"^\d{5}(-\d{4})?$");
    }
}
