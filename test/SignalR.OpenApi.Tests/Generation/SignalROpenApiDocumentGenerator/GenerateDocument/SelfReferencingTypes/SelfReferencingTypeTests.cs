// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.SelfReferencingTypes;

/// <summary>
/// Tests that the schema walk terminates for types that reference themselves.
/// A missing guard here does not fail an assertion — it overflows the stack and
/// takes down the whole process.
/// </summary>
[TestClass]
public class SelfReferencingTypeTests
{
    /// <summary>
    /// Verifies a plain self-referencing type terminates and is registered once.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SelfReferencingType_DoesNotStackOverflow()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(SelfReferencingHub));

        Assert.IsTrue(doc.Paths.ContainsKey("/hubs/SelfReferencing/ProcessNode"));
        Assert.IsTrue(doc.Components.Schemas.ContainsKey("TreeNode"));
        GeneratorTestHelper.AssertAllSchemaReferencesResolve(doc);
    }

    /// <summary>
    /// Verifies a polymorphic type that lists itself as one of its own derived types
    /// terminates, and that the base type is emitted as a usable variant rather than
    /// being dropped.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_PolymorphicTypeListingItselfAsDerivedType_DoesNotStackOverflow()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(SelfReferencingHub));

        Assert.IsTrue(
            doc.Paths.ContainsKey("/hubs/SelfReferencing/SubmitA"),
            "The operation for the self-listed polymorphic type should be generated.");

        var submitA = doc.Paths["/hubs/SelfReferencing/SubmitA"].Operations[OperationType.Post];

        Assert.IsNotNull(submitA.RequestBody);
        var schema = submitA.RequestBody.Content["application/json"].Schema;

        Assert.IsNotNull(schema.OneOf, "Polymorphic schema should have OneOf.");
        Assert.AreEqual(2, schema.OneOf.Count, "Both the base type and the derived type should be variants.");
        Assert.IsNotNull(schema.Discriminator, "Should have a discriminator.");
        Assert.AreEqual("kind", schema.Discriminator.PropertyName);

        Assert.IsNotNull(schema.Discriminator.Mapping);
        Assert.AreEqual("#/components/schemas/ClassA", schema.Discriminator.Mapping["ClassA"]);
        Assert.AreEqual("#/components/schemas/ClassB", schema.Discriminator.Mapping["ClassB"]);

        Assert.IsTrue(doc.Components.Schemas.ContainsKey("ClassA"));
        Assert.IsTrue(doc.Components.Schemas.ContainsKey("ClassB"));

        // The base type's own schema must be a real object schema, not an empty stub.
        var classA = doc.Components.Schemas["ClassA"];
        Assert.IsNotNull(classA.Properties, "ClassA schema should have properties.");
        Assert.IsTrue(classA.Properties.ContainsKey("name"), "ClassA schema should keep its own properties.");

        Assert.IsTrue(
            classA.Properties.ContainsKey("kind"),
            "ClassA schema should carry the discriminator property for its own variant.");
        var discriminatorEnum = classA.Properties["kind"].Enum;
        Assert.IsNotNull(discriminatorEnum);
        Assert.AreEqual(
            "ClassA",
            ((Microsoft.OpenApi.Any.OpenApiString)discriminatorEnum[0]).Value,
            "The base type's discriminator value should be its own name.");
    }

    /// <summary>
    /// Verifies a polymorphic type reachable from itself through a property terminates
    /// and leaves no dangling reference behind.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_PolymorphicTypeReferencedByDerivedTypeProperty_DoesNotStackOverflow()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(SelfReferencingHub));

        Assert.IsTrue(
            doc.Paths.ContainsKey("/hubs/SelfReferencing/SubmitC"),
            "The operation for the cyclic polymorphic type should be generated.");

        Assert.IsTrue(doc.Components.Schemas.ContainsKey("ClassC"));
        Assert.IsTrue(doc.Components.Schemas.ContainsKey("ClassD"));

        var classD = doc.Components.Schemas["ClassD"];
        Assert.IsNotNull(classD.Properties);
        Assert.IsTrue(classD.Properties.ContainsKey("next"), "ClassD should expose the cyclic property.");

        // Terminating the cycle must not leave behind a reference to a schema that was
        // never registered.
        GeneratorTestHelper.AssertAllSchemaReferencesResolve(doc);
    }
}
