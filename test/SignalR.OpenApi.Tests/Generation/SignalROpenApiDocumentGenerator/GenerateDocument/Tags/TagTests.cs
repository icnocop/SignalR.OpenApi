// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Tags;

/// <summary>
/// Tests for document-level tags and their descriptions.
/// </summary>
[TestClass]
public class TagTests
{
    /// <summary>
    /// Verifies the document tag list contains both the default hub tag and custom tags.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_PopulatesDocumentLevelTags()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(DocumentedTagHub));

        Assert.IsNotNull(doc.Tags, "Document should have tags.");
        var tagNames = doc.Tags.Select(t => t.Name).ToList();

        CollectionAssert.Contains(tagNames, "DocumentedTag", "Methods without [Tags] default to the hub name.");
        CollectionAssert.Contains(tagNames, "Users", "Methods with [Tags] contribute their custom tag.");
    }

    /// <summary>
    /// Verifies document tags are deduplicated when several methods share a tag.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_DeduplicatesDocumentTags()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(DocumentedTagHub));

        Assert.IsNotNull(doc.Tags);
        var tagNames = doc.Tags.Select(t => t.Name).ToList();
        CollectionAssert.AreEquivalent(
            tagNames.Distinct().ToList(),
            tagNames,
            "Document tags should be unique.");
    }

    /// <summary>
    /// Verifies a tag with no configured description falls back to the hub's XML summary.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenNoTagDescriptionThenFallsBackToHubSummary()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(DocumentedTagHub));

        Assert.IsNotNull(doc.Tags);
        var hubTag = doc.Tags.FirstOrDefault(t => t.Name == "DocumentedTag");
        Assert.IsNotNull(hubTag);
        Assert.AreEqual("A documented hub for tag testing.", hubTag.Description);
    }

    /// <summary>
    /// Verifies a configured tag description is applied.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenTagDescriptionConfiguredThenApplied()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o => o.TagDescriptions["Users"] = "User operations",
            typeof(DocumentedTagHub));

        Assert.IsNotNull(doc.Tags);
        var usersTag = doc.Tags.FirstOrDefault(t => t.Name == "Users");
        Assert.IsNotNull(usersTag);
        Assert.AreEqual("User operations", usersTag.Description);
    }

    /// <summary>
    /// Verifies a configured tag description wins over the hub's XML summary.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenTagDescriptionConfiguredThenOverridesHubSummary()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o => o.TagDescriptions["DocumentedTag"] = "Custom description",
            typeof(DocumentedTagHub));

        Assert.IsNotNull(doc.Tags);
        var hubTag = doc.Tags.FirstOrDefault(t => t.Name == "DocumentedTag");
        Assert.IsNotNull(hubTag);
        Assert.AreEqual("Custom description", hubTag.Description);
    }
}
