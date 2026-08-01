// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument;

/// <summary>
/// Assertions that must hold for the whole document regardless of hub shape.
/// </summary>
/// <remarks>
/// Unlike the scenario-specific test classes, these deliberately scan every hub in the
/// test assembly — the point is that no hub anywhere violates these invariants.
/// </remarks>
[TestClass]
public class DocumentInvariantsTests
{
    /// <summary>
    /// Verifies every non-event path exposes a POST operation.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_UsesPostForAllHubMethods()
    {
        var doc = GeneratorTestHelper.GenerateForAllHubs();

        foreach (var path in doc.Paths)
        {
            if (path.Key.Contains("/events/", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.IsTrue(
                path.Value.Operations.ContainsKey(OperationType.Post),
                $"Path {path.Key} should use POST.");
        }
    }

    /// <summary>
    /// Verifies every operation has an operation ID.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AllOperationsHaveOperationIds()
    {
        var doc = GeneratorTestHelper.GenerateForAllHubs();

        foreach (var path in doc.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                Assert.IsFalse(
                    string.IsNullOrEmpty(operation.Value.OperationId),
                    $"Path {path.Key} operation {operation.Key} should have an operationId.");
            }
        }
    }

    /// <summary>
    /// Verifies every response has a description, which OpenAPI requires.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AllResponsesHaveDescriptions()
    {
        var doc = GeneratorTestHelper.GenerateForAllHubs();

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

    /// <summary>
    /// Verifies document-level tags are unique across all hubs.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_DocumentTagsAreUnique()
    {
        var doc = GeneratorTestHelper.GenerateForAllHubs();

        Assert.IsNotNull(doc.Tags);
        var tagNames = doc.Tags.Select(t => t.Name).ToList();
        CollectionAssert.AreEquivalent(
            tagNames.Distinct().ToList(),
            tagNames,
            "Document tags should be unique.");
    }

    /// <summary>
    /// Verifies no schema reference anywhere in the document dangles.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AllSchemaReferencesResolve()
    {
        var doc = GeneratorTestHelper.GenerateForAllHubs();

        GeneratorTestHelper.AssertAllSchemaReferencesResolve(doc);
    }

    /// <summary>
    /// Verifies the full document, across every hub, serializes without error.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SerializesToValidJson()
    {
        var doc = GeneratorTestHelper.GenerateForAllHubs();

        var json = GeneratorTestHelper.SerializeToJson(doc);

        Assert.IsFalse(string.IsNullOrEmpty(json));
        Assert.IsTrue(json.Contains("\"openapi\"", StringComparison.Ordinal));
    }
}
