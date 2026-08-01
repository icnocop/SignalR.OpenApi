// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Tags;

/// <summary>
/// Tests the case where no operation tag matches the hub name. The x-signalr extension
/// must still identify the hub, because that is how the Swagger UI plugin maps a tag
/// group back to a connection.
/// </summary>
[TestClass]
public class AllCustomTagsTests
{
    /// <summary>
    /// Verifies operations use the custom tag while x-signalr keeps the hub name and route.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AllMethodsHaveCustomTags_ExtensionStillContainsHubInfo()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o => o.HubRoutes[typeof(AllCustomTagsHub)] = "/hubs/custom-tags",
            typeof(AllCustomTagsHub));

        var sayHello = doc.Paths["/hubs/AllCustomTags/SayHello"].Operations[OperationType.Post];
        var sayHelloTags = sayHello.Tags.Select(t => t.Name).ToList();
        CollectionAssert.Contains(sayHelloTags, "Greetings");
        CollectionAssert.DoesNotContain(sayHelloTags, "AllCustomTags");

        var sayGoodbye = doc.Paths["/hubs/AllCustomTags/SayGoodbye"].Operations[OperationType.Post];
        var sayGoodbyeTags = sayGoodbye.Tags.Select(t => t.Name).ToList();
        CollectionAssert.Contains(sayGoodbyeTags, "Greetings");
        CollectionAssert.DoesNotContain(sayGoodbyeTags, "AllCustomTags");

        var ext = (Microsoft.OpenApi.Any.OpenApiObject)sayHello.Extensions["x-signalr"];
        Assert.AreEqual("AllCustomTags", ((Microsoft.OpenApi.Any.OpenApiString)ext["hub"]).Value);
        Assert.AreEqual("/hubs/custom-tags", ((Microsoft.OpenApi.Any.OpenApiString)ext["hubPath"]).Value);
    }
}
