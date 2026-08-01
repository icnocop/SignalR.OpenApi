// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.ClientEvents;

/// <summary>
/// Tests the case where every client event has a custom tag, so the default
/// "{HubName} Events" tag should never be created.
/// </summary>
[TestClass]
public class AllTaggedClientEventTests
{
    /// <summary>
    /// Verifies the default events tag is omitted when all events are custom-tagged.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AllClientEventsHaveCustomTags_OmitsDefaultEventsTag()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(AllTaggedEventsHub));

        Assert.IsNotNull(doc.Tags);
        var tagNames = doc.Tags.Select(t => t.Name).ToList();

        CollectionAssert.DoesNotContain(tagNames, "AllTaggedEvents Events");
        CollectionAssert.Contains(tagNames, "Notifications");
    }
}
