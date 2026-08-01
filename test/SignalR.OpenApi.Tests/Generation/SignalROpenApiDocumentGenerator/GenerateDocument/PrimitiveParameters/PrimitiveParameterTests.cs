// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.PrimitiveParameters;

/// <summary>
/// Tests for hub methods whose parameters are all primitives. Each parameter becomes
/// a property of the request body, and both JSON and form-urlencoded are offered.
/// </summary>
[TestClass]
public class PrimitiveParameterTests
{
    /// <summary>
    /// Verifies a path is created for each hub method.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_CreatesPathPerHubMethod()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PrimitiveParameterHub));

        Assert.IsTrue(doc.Paths.ContainsKey("/hubs/PrimitiveParameter/SendMessage"));
        Assert.IsTrue(doc.Paths.ContainsKey("/hubs/PrimitiveParameter/GetTime"));
    }

    /// <summary>
    /// Verifies hub methods are modeled as POST operations.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_UsesPostForHubMethods()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PrimitiveParameterHub));

        Assert.IsTrue(doc.Paths["/hubs/PrimitiveParameter/SendMessage"]
            .Operations.ContainsKey(OperationType.Post));
    }

    /// <summary>
    /// Verifies the x-signalr extension is added to operations.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AddsSignalRExtension()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PrimitiveParameterHub));

        var sendMessage = doc.Paths["/hubs/PrimitiveParameter/SendMessage"]
            .Operations[OperationType.Post];

        Assert.IsTrue(sendMessage.Extensions.ContainsKey("x-signalr"));
    }

    /// <summary>
    /// Verifies a primitive parameter becomes a named property of the request body.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_GeneratesRequestBodySchema()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PrimitiveParameterHub));

        var sendMessage = doc.Paths["/hubs/PrimitiveParameter/SendMessage"]
            .Operations[OperationType.Post];

        Assert.IsNotNull(sendMessage.RequestBody);
        Assert.IsTrue(sendMessage.RequestBody.Content.ContainsKey("application/json"));

        var schema = sendMessage.RequestBody.Content["application/json"].Schema;
        Assert.AreEqual("object", schema.Type);
        Assert.IsTrue(schema.Properties.ContainsKey("message"));
    }

    /// <summary>
    /// Verifies multiple primitive parameters each become a named property.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_MultipleParams_WrappedSchema()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PrimitiveParameterHub));

        var sendToUser = doc.Paths["/hubs/PrimitiveParameter/SendToUser"]
            .Operations[OperationType.Post];

        var schema = sendToUser.RequestBody!.Content["application/json"].Schema;
        Assert.AreEqual("object", schema.Type);
        Assert.IsTrue(schema.Properties.ContainsKey("user"));
        Assert.IsTrue(schema.Properties.ContainsKey("message"));
        Assert.AreEqual(2, schema.Properties.Count);
    }

    /// <summary>
    /// Verifies primitive parameters support both JSON and form-urlencoded bodies.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_RequestBodyHasBothContentTypes()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PrimitiveParameterHub));

        var sendMessage = doc.Paths["/hubs/PrimitiveParameter/SendMessage"]
            .Operations[OperationType.Post];

        Assert.IsTrue(sendMessage.RequestBody!.Content.ContainsKey("application/json"));
        Assert.IsTrue(sendMessage.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"));
    }

    /// <summary>
    /// Verifies the x-signalr extension reports the parameter count.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ParameterCountInExtension()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PrimitiveParameterHub));

        var sendMessage = doc.Paths["/hubs/PrimitiveParameter/SendMessage"]
            .Operations[OperationType.Post];
        var sendMessageExt = (Microsoft.OpenApi.Any.OpenApiObject)sendMessage.Extensions["x-signalr"];
        Assert.AreEqual(1, ((Microsoft.OpenApi.Any.OpenApiInteger)sendMessageExt["parameterCount"]).Value);

        var getTime = doc.Paths["/hubs/PrimitiveParameter/GetTime"]
            .Operations[OperationType.Post];
        var getTimeExt = (Microsoft.OpenApi.Any.OpenApiObject)getTime.Extensions["x-signalr"];
        Assert.AreEqual(0, ((Microsoft.OpenApi.Any.OpenApiInteger)getTimeExt["parameterCount"]).Value);
    }

    /// <summary>
    /// Verifies flattenedBody is false for primitive parameters, since they are wrapped
    /// rather than flattened.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_FlattenedBodyIsFalseForPrimitiveParameters()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(PrimitiveParameterHub));

        var sendMessage = doc.Paths["/hubs/PrimitiveParameter/SendMessage"]
            .Operations[OperationType.Post];

        var ext = (Microsoft.OpenApi.Any.OpenApiObject)sendMessage.Extensions["x-signalr"];
        var flattenedBody = ((Microsoft.OpenApi.Any.OpenApiBoolean)ext["flattenedBody"]).Value;
        Assert.IsFalse(flattenedBody, "flattenedBody should be false for primitive parameters.");
    }
}
