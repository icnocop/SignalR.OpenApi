// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Responses;

/// <summary>
/// Tests mapping hub method return types onto OpenAPI responses.
/// </summary>
[TestClass]
public class ResponseSchemaTests
{
    /// <summary>
    /// Verifies a method returning a value produces a 200 response with a schema.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ReturnTypeMethod_Returns200WithSchema()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(ResponseHub));

        var echo = doc.Paths["/hubs/Response/Echo"].Operations[OperationType.Post];

        Assert.IsTrue(echo.Responses.ContainsKey("200"));
        var response = echo.Responses["200"];
        Assert.IsTrue(response.Content.ContainsKey("application/json"));
        Assert.AreEqual("string", response.Content["application/json"].Schema.Type);
    }

    /// <summary>
    /// Verifies a method returning a bare Task produces a 204 response.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_VoidMethod_Returns204()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(ResponseHub));

        var record = doc.Paths["/hubs/Response/Record"].Operations[OperationType.Post];

        Assert.IsTrue(record.Responses.ContainsKey("204"), "A method returning Task should produce 204.");
        Assert.IsFalse(record.Responses.ContainsKey("200"));
    }

    /// <summary>
    /// Verifies every response carries a description.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AllResponsesHaveDescriptions()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(ResponseHub));

        foreach (var path in doc.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                foreach (var response in operation.Value.Responses)
                {
                    Assert.IsFalse(
                        string.IsNullOrEmpty(response.Value.Description),
                        $"Path {path.Key} response {response.Key} should have a description.");
                }
            }
        }
    }
}
