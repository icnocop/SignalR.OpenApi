// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using FluentValidation;

namespace SignalR.OpenApi.FluentValidation.Tests.TestModels;

/// <summary>
/// Validator for <see cref="CreateOrderRequest"/> demonstrating various FluentValidation rules.
/// </summary>
public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOrderRequestValidator"/> class.
    /// </summary>
    public CreateOrderRequestValidator()
    {
        this.RuleFor(x => x.CustomerName)
            .NotEmpty()
            .Length(2, 100);

        this.RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        this.RuleFor(x => x.Quantity)
            .NotNull()
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(1000);

        this.RuleFor(x => x.Price)
            .GreaterThan(0);

        this.RuleFor(x => x.ProductCode)
            .NotEmpty()
            .Matches(@"^[A-Z]{2,4}-\d{3,6}$");

        this.RuleFor(x => x.Discount)
            .InclusiveBetween(0, 100);

        this.RuleFor(x => x.Priority)
            .ExclusiveBetween(0, 10);
    }
}
