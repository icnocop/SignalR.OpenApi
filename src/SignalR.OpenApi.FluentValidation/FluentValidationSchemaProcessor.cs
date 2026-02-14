// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace SignalR.OpenApi.FluentValidation;

/// <summary>
/// Processes OpenAPI schemas by applying FluentValidation rules as schema constraints.
/// Resolves <see cref="IValidator{T}"/> instances from the service provider and maps
/// validation rules to OpenAPI schema properties such as <c>required</c>, <c>minLength</c>,
/// <c>maxLength</c>, <c>pattern</c>, <c>minimum</c>, and <c>maximum</c>.
/// </summary>
public sealed class FluentValidationSchemaProcessor : ISignalROpenApiSchemaProcessor
{
    private static readonly string EmailPattern = @"^[^@]+@[^@]+$";

    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationSchemaProcessor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving validators.</param>
    public FluentValidationSchemaProcessor(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public void ProcessSchema(OpenApiSchema schema, Type type)
    {
        var validator = this.ResolveValidator(type);
        if (validator is null)
        {
            return;
        }

        this.ApplyValidatorRules(schema, validator);
    }

    private static void ApplyPropertyValidatorRules(OpenApiSchema schema, OpenApiSchema propertySchema, string propertyName, IPropertyValidator propertyValidator)
    {
        switch (propertyValidator)
        {
            case INotNullValidator:
            case INotEmptyValidator:
                schema.Required ??= new HashSet<string>();
                schema.Required.Add(propertyName);
                propertySchema.Nullable = false;
                if (propertyValidator is INotEmptyValidator && propertySchema.Type == "string")
                {
                    propertySchema.MinLength = Math.Max(propertySchema.MinLength ?? 0, 1);
                }

                break;

            case ILengthValidator lengthValidator:
                if (lengthValidator.Min > 0)
                {
                    propertySchema.MinLength = lengthValidator.Min;
                }

                if (lengthValidator.Max > 0 && lengthValidator.Max < int.MaxValue)
                {
                    propertySchema.MaxLength = lengthValidator.Max;
                }

                break;

            case IRegularExpressionValidator regexValidator:
                propertySchema.Pattern = regexValidator.Expression;
                break;

            case IComparisonValidator comparisonValidator:
                ApplyComparisonValidator(propertySchema, comparisonValidator);
                break;

            case IBetweenValidator betweenValidator:
                ApplyBetweenValidator(propertySchema, betweenValidator);
                break;

            default:
                if (IsAspNetCoreCompatibleEmailValidator(propertyValidator))
                {
                    propertySchema.Pattern = EmailPattern;
                }

                break;
        }
    }

    private static void ApplyComparisonValidator(OpenApiSchema propertySchema, IComparisonValidator comparisonValidator)
    {
        if (comparisonValidator.ValueToCompare is not IConvertible convertible)
        {
            return;
        }

        var value = convertible.ToDecimal(System.Globalization.CultureInfo.InvariantCulture);

        switch (comparisonValidator.Comparison)
        {
            case Comparison.GreaterThanOrEqual:
                propertySchema.Minimum = value;
                break;

            case Comparison.GreaterThan:
                propertySchema.Minimum = value;
                propertySchema.ExclusiveMinimum = true;
                break;

            case Comparison.LessThanOrEqual:
                propertySchema.Maximum = value;
                break;

            case Comparison.LessThan:
                propertySchema.Maximum = value;
                propertySchema.ExclusiveMaximum = true;
                break;
        }
    }

    private static void ApplyBetweenValidator(OpenApiSchema propertySchema, IBetweenValidator betweenValidator)
    {
        if (betweenValidator.From is IConvertible fromConvertible)
        {
            propertySchema.Minimum = fromConvertible.ToDecimal(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (betweenValidator.To is IConvertible toConvertible)
        {
            propertySchema.Maximum = toConvertible.ToDecimal(System.Globalization.CultureInfo.InvariantCulture);
        }

        // ExclusiveBetweenValidator is a concrete generic type, check by name
        if (betweenValidator.GetType().Name.StartsWith("ExclusiveBetweenValidator", StringComparison.Ordinal))
        {
            propertySchema.ExclusiveMinimum = true;
            propertySchema.ExclusiveMaximum = true;
        }
    }

    private static bool IsAspNetCoreCompatibleEmailValidator(IPropertyValidator validator)
    {
        return validator.GetType().Name.Contains("AspNetCoreCompatibleEmailValidator", StringComparison.Ordinal);
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return char.ToUpperInvariant(input[0]) + input[1..];
    }

    private void ApplyValidatorRules(OpenApiSchema schema, IValidator validator)
    {
        if (schema.Properties is null || schema.Properties.Count == 0)
        {
            return;
        }

        var descriptor = validator.CreateDescriptor();

        foreach (var (propertyName, propertySchema) in schema.Properties)
        {
            // Try camelCase first (as used in OpenAPI), then PascalCase (as used in C# properties)
            var rules = descriptor.GetRulesForMember(propertyName);
            if (rules is null || !rules.Any())
            {
                rules = descriptor.GetRulesForMember(ToPascalCase(propertyName));
            }

            if (rules is null || !rules.Any())
            {
                continue;
            }

            foreach (var rule in rules)
            {
                if (rule is IValidationRule validationRule)
                {
                    foreach (var component in validationRule.Components)
                    {
                        ApplyPropertyValidatorRules(schema, propertySchema, propertyName, component.Validator);
                    }
                }
            }

            // Handle included/child validators
            this.ApplyIncludedValidatorRules(schema, propertySchema, propertyName, descriptor);
        }
    }

    private void ApplyIncludedValidatorRules(OpenApiSchema parentSchema, OpenApiSchema propertySchema, string propertyName, IValidatorDescriptor descriptor)
    {
        // Look for child validators on this property
        var rules = descriptor.GetRulesForMember(propertyName);
        if (rules is null || !rules.Any())
        {
            rules = descriptor.GetRulesForMember(ToPascalCase(propertyName));
        }

        if (rules is null || !rules.Any())
        {
            return;
        }

        foreach (var rule in rules)
        {
            if (rule is not IValidationRule validationRule)
            {
                continue;
            }

            foreach (var component in validationRule.Components)
            {
                // Check for child validator adaptor (ChildValidatorAdaptor<,>)
                var validatorType = component.Validator.GetType();
                if (!validatorType.IsGenericType)
                {
                    continue;
                }

                var genericDef = validatorType.GetGenericTypeDefinition();
                if (genericDef.Name.StartsWith("ChildValidatorAdaptor", StringComparison.Ordinal))
                {
                    // Try to get the child validator type and resolve it
                    var typeArgs = validatorType.GetGenericArguments();
                    if (typeArgs.Length >= 2)
                    {
                        var childType = typeArgs[1];
                        var childValidator = this.ResolveValidator(childType);
                        if (childValidator is not null)
                        {
                            this.ApplyValidatorRules(propertySchema, childValidator);
                        }
                    }
                }
            }
        }
    }

    private IValidator? ResolveValidator(Type type)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(type);

        // Create a scope to resolve scoped validators (FluentValidation registers validators as scoped by default)
        using var scope = this.serviceProvider.CreateScope();
        return scope.ServiceProvider.GetService(validatorType) as IValidator;
    }
}
