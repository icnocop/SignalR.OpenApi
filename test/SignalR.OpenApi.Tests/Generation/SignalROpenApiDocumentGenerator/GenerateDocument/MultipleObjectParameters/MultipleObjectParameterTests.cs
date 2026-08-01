// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.MultipleObjectParameters;

/// <summary>
/// Tests for hub methods with more than one parameter where at least one is a complex
/// object. These are wrapped (each parameter becomes a named property) and are JSON-only,
/// because form-urlencoded cannot represent nested objects.
/// </summary>
[TestClass]
public class MultipleObjectParameterTests
{
    /// <summary>
    /// Verifies two complex object parameters produce a JSON-only wrapper schema.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_TwoObjectParameters_JsonOnly()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(MultipleObjectParameterHub));

        var compare = doc.Paths["/hubs/MultipleObjectParameter/CompareAddresses"]
            .Operations[OperationType.Post];

        Assert.IsTrue(compare.RequestBody!.Content.ContainsKey("application/json"));
        Assert.IsFalse(
            compare.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"),
            "Multiple complex object parameters should not offer form-urlencoded.");

        var schema = compare.RequestBody.Content["application/json"].Schema;
        Assert.AreEqual("object", schema.Type);
        Assert.IsTrue(schema.Properties.ContainsKey("first"));
        Assert.IsTrue(schema.Properties.ContainsKey("second"));
    }

    /// <summary>
    /// Verifies a complex object mixed with a primitive produces a JSON-only wrapper
    /// schema with both parameters as named properties.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ObjectAndStringParameters_WrappedJsonOnlySchema()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(MultipleObjectParameterHub));

        var ship = doc.Paths["/hubs/MultipleObjectParameter/ShipWithNote"]
            .Operations[OperationType.Post];

        Assert.IsNotNull(ship.RequestBody);
        Assert.IsTrue(ship.RequestBody.Content.ContainsKey("application/json"));
        Assert.IsFalse(
            ship.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"),
            "Mixed object and primitive parameters should not offer form-urlencoded.");

        var schema = ship.RequestBody.Content["application/json"].Schema;
        Assert.AreEqual("object", schema.Type);
        Assert.IsTrue(schema.Properties.ContainsKey("address"));
        Assert.IsTrue(schema.Properties.ContainsKey("note"));
        Assert.AreEqual(2, schema.Properties.Count);
        Assert.AreEqual("string", schema.Properties["note"].Type);

        var ext = (Microsoft.OpenApi.Any.OpenApiObject)ship.Extensions["x-signalr"];
        Assert.AreEqual(2, ((Microsoft.OpenApi.Any.OpenApiInteger)ext["parameterCount"]).Value);
        Assert.IsFalse(((Microsoft.OpenApi.Any.OpenApiBoolean)ext["flattenedBody"]).Value);
    }
}
