// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Streaming;

/// <summary>
/// Tests for streaming hub methods, which are flagged in the x-signalr extension so the
/// Swagger UI plugin can render them as streams rather than single invocations.
/// </summary>
[TestClass]
public class StreamingTests
{
    /// <summary>
    /// Verifies streaming methods set the x-signalr stream flag.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_StreamingMethodHasStreamFlag()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(StreamingMethodHub));

        var operation = doc.Paths["/hubs/StreamingMethod/StreamIntegers"]
            .Operations[OperationType.Post];
        var extension = (Microsoft.OpenApi.Any.OpenApiObject)operation.Extensions["x-signalr"];

        Assert.IsTrue(((Microsoft.OpenApi.Any.OpenApiBoolean)extension["stream"]).Value);
    }

    /// <summary>
    /// Verifies non-streaming methods do not set the stream flag.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_NonStreamingMethodDoesNotHaveStreamFlag()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(StreamingMethodHub));

        var operation = doc.Paths["/hubs/StreamingMethod/GetSingleValue"]
            .Operations[OperationType.Post];
        var extension = (Microsoft.OpenApi.Any.OpenApiObject)operation.Extensions["x-signalr"];

        Assert.IsFalse(((Microsoft.OpenApi.Any.OpenApiBoolean)extension["stream"]).Value);
    }

    /// <summary>
    /// Verifies the response is an array of the streamed item type, rather than the
    /// <see cref="IAsyncEnumerable{T}"/> wrapper itself.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_StreamingMethodResponseIsArrayOfItemType()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(StreamingMethodHub));

        var operation = doc.Paths["/hubs/StreamingMethod/StreamIntegers"]
            .Operations[OperationType.Post];

        var schema = operation.Responses["200"].Content["application/json"].Schema;
        Assert.AreEqual("array", schema.Type, "A stream should be modeled as an array.");
        Assert.IsNotNull(schema.Items);
        Assert.AreEqual("integer", schema.Items.Type, "The array items should be the streamed item type.");
    }
}
