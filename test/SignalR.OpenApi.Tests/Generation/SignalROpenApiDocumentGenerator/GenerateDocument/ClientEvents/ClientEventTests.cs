// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.ClientEvents;

/// <summary>
/// Tests for client events on strongly typed hubs. Events are modeled as GET operations
/// under <c>/hubs/{Hub}/events/{Event}</c> and carry extra x-signalr metadata the Swagger
/// UI plugin needs to render received payloads.
/// </summary>
[TestClass]
public class ClientEventTests
{
    /// <summary>
    /// Verifies client events are emitted as GET operations.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ClientEventsAreGetOperations()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(EventHub));

        Assert.IsTrue(doc.Paths.ContainsKey("/hubs/Event/events/ReceiveMessage"));
        Assert.IsTrue(doc.Paths["/hubs/Event/events/ReceiveMessage"]
            .Operations.ContainsKey(OperationType.Get));
    }

    /// <summary>
    /// Verifies the x-signalr extension maps positional SignalR arguments to names.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ClientEventIncludesParameterNames()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(EventHub));

        var eventOp = doc.Paths["/hubs/Event/events/ReceiveMessage"]
            .Operations[OperationType.Get];

        var ext = (Microsoft.OpenApi.Any.OpenApiObject)eventOp.Extensions["x-signalr"];
        var paramNames = (Microsoft.OpenApi.Any.OpenApiArray)ext["parameterNames"];

        Assert.AreEqual(2, paramNames.Count);
        Assert.AreEqual("user", ((Microsoft.OpenApi.Any.OpenApiString)paramNames[0]).Value);
        Assert.AreEqual("message", ((Microsoft.OpenApi.Any.OpenApiString)paramNames[1]).Value);
    }

    /// <summary>
    /// Verifies polymorphic event payloads carry eventDiscriminators metadata, which the
    /// plugin uses to infer the type because SignalR omits the discriminator on the wire.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_PolymorphicClientEventIncludesEventDiscriminators()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(EventHub));

        var eventOp = doc.Paths["/hubs/Event/events/NotificationRaised"]
            .Operations[OperationType.Get];

        var ext = (Microsoft.OpenApi.Any.OpenApiObject)eventOp.Extensions["x-signalr"];
        Assert.IsTrue(ext.ContainsKey("eventDiscriminators"), "Should have eventDiscriminators.");

        var discriminators = (Microsoft.OpenApi.Any.OpenApiObject)ext["eventDiscriminators"];
        Assert.IsTrue(discriminators.ContainsKey("notification"));

        var notification = (Microsoft.OpenApi.Any.OpenApiObject)discriminators["notification"];
        Assert.AreEqual("kind", ((Microsoft.OpenApi.Any.OpenApiString)notification["property"]).Value);

        var mapping = (Microsoft.OpenApi.Any.OpenApiObject)notification["mapping"];
        Assert.IsTrue(mapping.ContainsKey("text"));
        Assert.IsTrue(mapping.ContainsKey("alert"));
    }

    /// <summary>
    /// Verifies an event with a custom [Tags] attribute uses that tag instead of the default.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ClientEventWithTagsAttribute_UsesCustomTag()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(EventHub));

        var operation = doc.Paths["/hubs/Event/events/UserJoined"].Operations[OperationType.Get];
        var tagNames = operation.Tags.Select(t => t.Name).ToList();

        CollectionAssert.Contains(tagNames, "Presence");
        CollectionAssert.DoesNotContain(tagNames, "Event Events");
    }

    /// <summary>
    /// Verifies an event without [Tags] falls back to the "{HubName} Events" tag.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ClientEventWithoutTagsAttribute_UsesDefaultTag()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(EventHub));

        var operation = doc.Paths["/hubs/Event/events/ReceiveMessage"].Operations[OperationType.Get];
        var tagNames = operation.Tags.Select(t => t.Name).ToList();

        CollectionAssert.Contains(tagNames, "Event Events");
    }

    /// <summary>
    /// Verifies client event tags reach the document-level tag list.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_IncludesClientEventTagsInDocumentTags()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(EventHub));

        Assert.IsNotNull(doc.Tags);
        var tagNames = doc.Tags.Select(t => t.Name).ToList();
        CollectionAssert.Contains(tagNames, "Event Events");
    }
}
