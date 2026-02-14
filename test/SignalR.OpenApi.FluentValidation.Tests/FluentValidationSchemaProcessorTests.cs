// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignalR.OpenApi.FluentValidation.Tests.TestModels;

namespace SignalR.OpenApi.FluentValidation.Tests;

/// <summary>
/// Tests for <see cref="FluentValidationSchemaProcessor"/>.
/// </summary>
[TestClass]
public class FluentValidationSchemaProcessorTests
{
    /// <summary>
    /// Tests that NotEmpty rule adds property to required set and sets minLength.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_NotEmpty_AddsRequiredAndMinLength()
    {
        using var sp = CreateServiceProvider();
        var processor = new FluentValidationSchemaProcessor(sp);
        var schema = CreateSchemaForType(typeof(CreateOrderRequest));

        processor.ProcessSchema(schema, typeof(CreateOrderRequest));

        Assert.IsTrue(schema.Required.Contains("customerName"));
        Assert.AreEqual(2, schema.Properties["customerName"].MinLength);
    }

    /// <summary>
    /// Tests that NotNull rule adds property to required set.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_NotNull_AddsRequired()
    {
        using var sp = CreateServiceProvider();
        var processor = new FluentValidationSchemaProcessor(sp);
        var schema = CreateSchemaForType(typeof(CreateOrderRequest));

        processor.ProcessSchema(schema, typeof(CreateOrderRequest));

        Assert.IsTrue(schema.Required.Contains("quantity"));
    }

    /// <summary>
    /// Tests that Length rule sets minLength and maxLength.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_Length_SetsMinAndMaxLength()
    {
        using var sp = CreateServiceProvider();
        var processor = new FluentValidationSchemaProcessor(sp);
        var schema = CreateSchemaForType(typeof(CreateOrderRequest));

        processor.ProcessSchema(schema, typeof(CreateOrderRequest));

        Assert.AreEqual(2, schema.Properties["customerName"].MinLength);
        Assert.AreEqual(100, schema.Properties["customerName"].MaxLength);
    }

    /// <summary>
    /// Tests that Matches rule sets pattern.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_Matches_SetsPattern()
    {
        using var sp = CreateServiceProvider();
        var processor = new FluentValidationSchemaProcessor(sp);
        var schema = CreateSchemaForType(typeof(CreateOrderRequest));

        processor.ProcessSchema(schema, typeof(CreateOrderRequest));

        Assert.AreEqual(@"^[A-Z]{2,4}-\d{3,6}$", schema.Properties["productCode"].Pattern);
    }

    /// <summary>
    /// Tests that GreaterThan rule sets minimum with exclusive flag.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_GreaterThan_SetsExclusiveMinimum()
    {
        using var sp = CreateServiceProvider();
        var processor = new FluentValidationSchemaProcessor(sp);
        var schema = CreateSchemaForType(typeof(CreateOrderRequest));

        processor.ProcessSchema(schema, typeof(CreateOrderRequest));

        Assert.AreEqual(0m, schema.Properties["price"].Minimum);
        Assert.IsTrue(schema.Properties["price"].ExclusiveMinimum ?? false);
    }

    /// <summary>
    /// Tests that GreaterThanOrEqualTo and LessThanOrEqualTo set minimum and maximum.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_Comparison_SetsMinimumAndMaximum()
    {
        using var sp = CreateServiceProvider();
        var processor = new FluentValidationSchemaProcessor(sp);
        var schema = CreateSchemaForType(typeof(CreateOrderRequest));

        processor.ProcessSchema(schema, typeof(CreateOrderRequest));

        Assert.AreEqual(1m, schema.Properties["quantity"].Minimum);
        Assert.AreEqual(1000m, schema.Properties["quantity"].Maximum);
        Assert.IsNull(schema.Properties["quantity"].ExclusiveMinimum);
        Assert.IsNull(schema.Properties["quantity"].ExclusiveMaximum);
    }

    /// <summary>
    /// Tests that InclusiveBetween sets minimum and maximum without exclusive flags.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_InclusiveBetween_SetsRange()
    {
        using var sp = CreateServiceProvider();
        var processor = new FluentValidationSchemaProcessor(sp);
        var schema = CreateSchemaForType(typeof(CreateOrderRequest));

        processor.ProcessSchema(schema, typeof(CreateOrderRequest));

        Assert.AreEqual(0m, schema.Properties["discount"].Minimum);
        Assert.AreEqual(100m, schema.Properties["discount"].Maximum);
        Assert.IsNull(schema.Properties["discount"].ExclusiveMinimum);
        Assert.IsNull(schema.Properties["discount"].ExclusiveMaximum);
    }

    /// <summary>
    /// Tests that ExclusiveBetween sets minimum and maximum with exclusive flags.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_ExclusiveBetween_SetsExclusiveRange()
    {
        using var sp = CreateServiceProvider();
        var processor = new FluentValidationSchemaProcessor(sp);
        var schema = CreateSchemaForType(typeof(CreateOrderRequest));

        processor.ProcessSchema(schema, typeof(CreateOrderRequest));

        Assert.AreEqual(0m, schema.Properties["priority"].Minimum);
        Assert.AreEqual(10m, schema.Properties["priority"].Maximum);
        Assert.IsTrue(schema.Properties["priority"].ExclusiveMinimum ?? false);
        Assert.IsTrue(schema.Properties["priority"].ExclusiveMaximum ?? false);
    }

    /// <summary>
    /// Tests that EmailAddress rule sets a pattern for email validation.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_EmailAddress_SetsPattern()
    {
        using var sp = CreateServiceProvider();
        var processor = new FluentValidationSchemaProcessor(sp);
        var schema = CreateSchemaForType(typeof(CreateOrderRequest));

        processor.ProcessSchema(schema, typeof(CreateOrderRequest));

        Assert.IsNotNull(schema.Properties["email"].Pattern);
        Assert.IsTrue(schema.Properties["email"].Pattern!.Contains("@"));
    }

    /// <summary>
    /// Tests that no changes are made when no validator is registered.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_NoValidator_LeavesSchemaUnchanged()
    {
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();
        var processor = new FluentValidationSchemaProcessor(serviceProvider);

        var schema = CreateSchemaForType(typeof(CreateOrderRequest));
        var originalPropertyCount = schema.Properties.Count;

        processor.ProcessSchema(schema, typeof(CreateOrderRequest));

        Assert.AreEqual(originalPropertyCount, schema.Properties.Count);
        Assert.AreEqual(0, schema.Required?.Count ?? 0);
    }

    /// <summary>
    /// Tests that schemas without properties are handled gracefully.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_NoProperties_DoesNotThrow()
    {
        using var sp = CreateServiceProvider();
        var processor = new FluentValidationSchemaProcessor(sp);

        var emptySchema = new OpenApiSchema
        {
            Type = "string",
        };

        processor.ProcessSchema(emptySchema, typeof(CreateOrderRequest));
    }

    /// <summary>
    /// Tests that the processor implements <see cref="ISignalROpenApiSchemaProcessor"/>.
    /// </summary>
    [TestMethod]
    public void FluentValidationSchemaProcessor_ImplementsInterface()
    {
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();
        var processor = new FluentValidationSchemaProcessor(serviceProvider);

        Assert.IsInstanceOfType<ISignalROpenApiSchemaProcessor>(processor);
    }

    /// <summary>
    /// Tests that MaximumLength sets maxLength on the property schema.
    /// </summary>
    [TestMethod]
    public void ProcessSchema_MaximumLength_SetsMaxLength()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<Address>, AddressValidator>();
        using var serviceProvider = services.BuildServiceProvider();
        var processor = new FluentValidationSchemaProcessor(serviceProvider);

        var schema = CreateSchemaForType(typeof(Address));

        processor.ProcessSchema(schema, typeof(Address));

        Assert.AreEqual(200, schema.Properties["street"].MaxLength);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
        services.AddSingleton<IValidator<Address>, AddressValidator>();
        return services.BuildServiceProvider();
    }

    private static OpenApiSchema CreateSchemaForType(Type type)
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>(),
            Required = new HashSet<string>(),
        };

        foreach (var prop in type.GetProperties())
        {
            var propName = char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];
            schema.Properties[propName] = new OpenApiSchema
            {
                Type = GetSchemaType(prop.PropertyType),
            };
        }

        return schema;
    }

    private static string GetSchemaType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string))
        {
            return "string";
        }

        if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short))
        {
            return "integer";
        }

        if (underlying == typeof(decimal) || underlying == typeof(float) || underlying == typeof(double))
        {
            return "number";
        }

        if (underlying == typeof(bool))
        {
            return "boolean";
        }

        return "object";
    }
}
