// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Polymorphism;

/// <summary>
/// Tests for polymorphic parameters. The main endpoint is JSON-only with a
/// <c>oneOf</c> schema and a discriminator; one flat, form-friendly sub-endpoint
/// is generated per derived type.
/// </summary>
[TestClass]
public class PolymorphicSchemaTests
{
    private const string MainPath = "/hubs/PolymorphicParameter/DrawShape";
    private const string CirclePath = "/hubs/PolymorphicParameter/DrawShape/circle";
    private const string RectanglePath = "/hubs/PolymorphicParameter/DrawShape/rectangle";

    /// <summary>
    /// Verifies a polymorphic parameter generates oneOf with a discriminator mapping.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_GeneratesOneOfWithDiscriminator()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PolymorphicParameterHub));

        var drawShape = doc.Paths[MainPath].Operations[OperationType.Post];

        Assert.IsNotNull(drawShape.RequestBody);
        var schema = drawShape.RequestBody.Content["application/json"].Schema;

        Assert.IsNotNull(schema.OneOf, "Polymorphic schema should have OneOf.");
        Assert.AreEqual(2, schema.OneOf.Count, "Should have 2 derived types (circle, rectangle).");
        Assert.IsNotNull(schema.Discriminator, "Should have a discriminator.");
        Assert.AreEqual("kind", schema.Discriminator.PropertyName);

        Assert.IsTrue(doc.Components.Schemas.ContainsKey("CircleDrawing"));
        Assert.IsTrue(doc.Components.Schemas.ContainsKey("RectangleDrawing"));

        Assert.IsNotNull(schema.Discriminator.Mapping, "Discriminator should have mapping.");
        Assert.AreEqual("#/components/schemas/CircleDrawing", schema.Discriminator.Mapping["circle"]);
        Assert.AreEqual("#/components/schemas/RectangleDrawing", schema.Discriminator.Mapping["rectangle"]);
    }

    /// <summary>
    /// Verifies sub-endpoints are generated per derived type, that the main endpoint
    /// stays JSON-only, and that sub-endpoints are form-friendly.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_GeneratesSubEndpointsPerDerivedType()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PolymorphicParameterHub));

        var drawShape = doc.Paths[MainPath].Operations[OperationType.Post];
        Assert.IsTrue(drawShape.RequestBody!.Content.ContainsKey("application/json"));
        Assert.IsFalse(
            drawShape.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"),
            "Main polymorphic endpoint should not have form-urlencoded.");

        Assert.IsTrue(doc.Paths.ContainsKey(CirclePath), "Should have circle sub-endpoint.");
        Assert.IsTrue(doc.Paths.ContainsKey(RectanglePath), "Should have rectangle sub-endpoint.");

        var circleOp = doc.Paths[CirclePath].Operations[OperationType.Post];
        Assert.IsTrue(circleOp.RequestBody!.Content.ContainsKey("application/json"));
        Assert.IsTrue(circleOp.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"));

        var circleExt = (Microsoft.OpenApi.Any.OpenApiObject)circleOp.Extensions["x-signalr"];
        Assert.AreEqual("DrawShape", ((Microsoft.OpenApi.Any.OpenApiString)circleExt["method"]).Value);
        Assert.AreEqual("kind", ((Microsoft.OpenApi.Any.OpenApiString)circleExt["discriminatorProperty"]).Value);
        Assert.AreEqual("circle", ((Microsoft.OpenApi.Any.OpenApiString)circleExt["discriminatorValue"]).Value);
    }

    /// <summary>
    /// Verifies the discriminator is visible in the JSON schema when
    /// IncludeDiscriminatorInExamples is true (the default).
    /// </summary>
    [TestMethod]
    public void GenerateDocument_IncludeDiscriminatorInExamples_DiscriminatorVisibleInJson()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o => o.IncludeDiscriminatorInExamples = true,
            typeof(PolymorphicParameterHub));

        var circleOp = doc.Paths[CirclePath].Operations[OperationType.Post];

        var jsonSchema = circleOp.RequestBody!.Content["application/json"].Schema;
        Assert.IsTrue(jsonSchema.Properties.ContainsKey("kind"), "JSON schema should contain discriminator.");
        Assert.IsFalse(jsonSchema.Properties["kind"].ReadOnly, "Discriminator should not be read-only in JSON schema.");
    }

    /// <summary>
    /// Verifies the discriminator is hidden from form inputs even when it is visible
    /// in JSON examples.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_IncludeDiscriminatorInExamples_DiscriminatorHiddenInForm()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o => o.IncludeDiscriminatorInExamples = true,
            typeof(PolymorphicParameterHub));

        var circleOp = doc.Paths[CirclePath].Operations[OperationType.Post];

        var formSchema = circleOp.RequestBody!.Content["application/x-www-form-urlencoded"].Schema;
        Assert.IsTrue(formSchema.Properties.ContainsKey("kind"), "Form schema should contain discriminator.");
        Assert.IsTrue(formSchema.Properties["kind"].ReadOnly, "Discriminator should be read-only in form schema.");
    }

    /// <summary>
    /// Verifies the discriminator is read-only in the JSON schema too when
    /// IncludeDiscriminatorInExamples is false.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ExcludeDiscriminatorFromExamples_DiscriminatorHidden()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o => o.IncludeDiscriminatorInExamples = false,
            typeof(PolymorphicParameterHub));

        var circleOp = doc.Paths[CirclePath].Operations[OperationType.Post];

        var jsonSchema = circleOp.RequestBody!.Content["application/json"].Schema;
        Assert.IsTrue(
            jsonSchema.Properties["kind"].ReadOnly,
            "Discriminator should be read-only when excluded from examples.");
    }

    /// <summary>
    /// Verifies each sub-endpoint carries only the examples matching its derived type,
    /// while the main endpoint carries all of them.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SubEndpointIncludesFilteredExamples()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PolymorphicParameterHub));

        var mainExamples = doc.Paths[MainPath].Operations[OperationType.Post]
            .RequestBody!.Content["application/json"].Examples;
        Assert.IsNotNull(mainExamples);
        Assert.AreEqual(2, mainExamples.Count, "Main endpoint should have both examples.");

        var circleExamples = doc.Paths[CirclePath].Operations[OperationType.Post]
            .RequestBody!.Content["application/json"].Examples;
        Assert.IsNotNull(circleExamples);
        Assert.AreEqual(1, circleExamples.Count, "Circle sub-endpoint should have only the circle example.");
        Assert.IsTrue(circleExamples.ContainsKey("SmallCircle"));

        var rectExamples = doc.Paths[RectanglePath].Operations[OperationType.Post]
            .RequestBody!.Content["application/json"].Examples;
        Assert.IsNotNull(rectExamples);
        Assert.AreEqual(1, rectExamples.Count, "Rectangle sub-endpoint should have only the rectangle example.");
        Assert.IsTrue(rectExamples.ContainsKey("LargeRectangle"));
    }

    /// <summary>
    /// Verifies sub-endpoint form schemas carry per-property examples filtered by
    /// derived type, and that the read-only discriminator gets no example.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SubEndpointFormSchemaHasFilteredPropertyExamples()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PolymorphicParameterHub));

        var circleOp = doc.Paths[CirclePath].Operations[OperationType.Post];
        var circleFormSchema = circleOp.RequestBody!.Content["application/x-www-form-urlencoded"].Schema;

        Assert.IsNotNull(circleFormSchema.Properties, "Circle form schema should have properties.");
        Assert.IsNotNull(circleFormSchema.Properties["color"].Example, "Color property should have an example.");
        Assert.IsNotNull(circleFormSchema.Properties["radius"].Example, "Radius property should have an example.");

        Assert.IsTrue(circleFormSchema.Properties["kind"].ReadOnly, "Discriminator should be readOnly.");
        Assert.IsNull(
            circleFormSchema.Properties["kind"].Example,
            "ReadOnly discriminator should not have an example.");
    }
}
