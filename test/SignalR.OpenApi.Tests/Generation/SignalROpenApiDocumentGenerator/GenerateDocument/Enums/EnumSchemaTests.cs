// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Enums;

/// <summary>
/// Tests for enum schema generation, which follows the configured
/// <see cref="System.Text.Json.JsonSerializerOptions"/>.
/// </summary>
[TestClass]
public class EnumSchemaTests
{
    /// <summary>
    /// Verifies enums default to integer schemas with numeric values.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenNoJsonStringEnumConverterThenEnumSchemaIsInteger()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(EnumHub));

        var getStatus = doc.Paths["/hubs/Enum/GetStatus"].Operations[OperationType.Post];

        var responseSchema = getStatus.Responses["200"].Content["application/json"].Schema;
        Assert.AreEqual("integer", responseSchema.Type, "Enum schema should be integer by default.");
        Assert.IsNotNull(responseSchema.Enum);
        Assert.AreEqual(4, responseSchema.Enum.Count, "WorkflowStatus has 4 values.");
        Assert.IsInstanceOfType(responseSchema.Enum[0], typeof(Microsoft.OpenApi.Any.OpenApiInteger));
    }

    /// <summary>
    /// Verifies enums become string schemas with names when a
    /// <see cref="JsonStringEnumConverter"/> is configured.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenJsonStringEnumConverterConfiguredThenEnumSchemaIsString()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()),
            typeof(EnumHub));

        var getStatus = doc.Paths["/hubs/Enum/GetStatus"].Operations[OperationType.Post];

        var responseSchema = getStatus.Responses["200"].Content["application/json"].Schema;
        Assert.AreEqual("string", responseSchema.Type);
        Assert.IsNotNull(responseSchema.Enum);
        Assert.AreEqual(4, responseSchema.Enum.Count);
        Assert.IsInstanceOfType(responseSchema.Enum[0], typeof(Microsoft.OpenApi.Any.OpenApiString));

        var enumNames = responseSchema.Enum
            .Cast<Microsoft.OpenApi.Any.OpenApiString>()
            .Select(e => e.Value)
            .ToList();
        CollectionAssert.Contains(enumNames, "Pending");
        CollectionAssert.Contains(enumNames, "Active");
        CollectionAssert.Contains(enumNames, "Completed");
        CollectionAssert.Contains(enumNames, "Failed");
    }

    /// <summary>
    /// Verifies the converter also applies to enum properties nested in complex types.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenJsonStringEnumConverterConfiguredThenEnumPropertyInObjectIsString()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()),
            typeof(EnumHub));

        Assert.IsTrue(doc.Components.Schemas.ContainsKey("WorkflowResult"));
        var resultSchema = doc.Components.Schemas["WorkflowResult"];
        Assert.IsTrue(resultSchema.Properties.ContainsKey("status"));
        Assert.AreEqual("string", resultSchema.Properties["status"].Type);
    }
}
