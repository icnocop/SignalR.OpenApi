// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.DocumentMetadata;

/// <summary>
/// Tests for top-level document metadata driven by options.
/// </summary>
[TestClass]
public class DocumentMetadataTests
{
    /// <summary>
    /// Verifies the configured title and version are used.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_UsesConfiguredTitleAndVersion()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o =>
            {
                o.DocumentTitle = "Test API";
                o.DocumentVersion = "v2";
            },
            typeof(MetadataHub));

        Assert.AreEqual("Test API", doc.Info.Title);
        Assert.AreEqual("v2", doc.Info.Version);
    }

    /// <summary>
    /// Verifies the document serializes to JSON containing the OpenAPI envelope.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SerializesToValidJson()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(MetadataHub));

        var json = GeneratorTestHelper.SerializeToJson(doc);

        Assert.IsFalse(string.IsNullOrEmpty(json));
        Assert.IsTrue(json.Contains("\"openapi\"", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("\"paths\"", StringComparison.Ordinal));
    }
}
