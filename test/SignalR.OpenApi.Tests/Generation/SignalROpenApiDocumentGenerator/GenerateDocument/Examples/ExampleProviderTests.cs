// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Examples;

/// <summary>
/// Tests for request and response examples supplied by
/// <see cref="SignalR.OpenApi.Examples.ISignalROpenApiExamplesProvider{T}"/> implementations.
/// </summary>
[TestClass]
public class ExampleProviderTests
{
    private const string CreateOrderPath = "/hubs/Example/CreateOrder";
    private const string GreetPath = "/hubs/Example/Greet";

    /// <summary>
    /// Verifies request examples are attached to the request body.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_RequestExamplesFromProvider()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(ExampleHub));

        var createOrder = doc.Paths[CreateOrderPath].Operations[OperationType.Post];

        Assert.IsNotNull(createOrder.RequestBody);
        var jsonContent = createOrder.RequestBody.Content["application/json"];
        Assert.IsNotNull(jsonContent.Examples);
        Assert.IsTrue(jsonContent.Examples.ContainsKey("SingleItem"));
        Assert.IsTrue(jsonContent.Examples.ContainsKey("BulkOrder"));
        Assert.AreEqual("Single item order", jsonContent.Examples["SingleItem"].Summary);
    }

    /// <summary>
    /// Verifies response examples are attached to the 200 response.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ResponseExamplesFromProvider()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(ExampleHub));

        var createOrder = doc.Paths[CreateOrderPath].Operations[OperationType.Post];

        var jsonContent = createOrder.Responses["200"].Content["application/json"];
        Assert.IsNotNull(jsonContent.Examples);
        Assert.IsTrue(jsonContent.Examples.ContainsKey("Created"));
        Assert.IsTrue(jsonContent.Examples.ContainsKey("Pending"));
    }

    /// <summary>
    /// Verifies response examples for a simple return type are attached.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SimpleTypeResponseExamples()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(ExampleHub));

        var greet = doc.Paths[GreetPath].Operations[OperationType.Post];

        var jsonContent = greet.Responses["200"].Content["application/json"];
        Assert.IsNotNull(jsonContent.Examples);
        Assert.IsTrue(jsonContent.Examples.ContainsKey("Casual"));
        Assert.IsTrue(jsonContent.Examples.ContainsKey("Formal"));
        Assert.AreEqual("Casual greeting", jsonContent.Examples["Casual"].Summary);
    }

    /// <summary>
    /// Verifies complex-object response examples survive serialization to JSON.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ResponseExamplesAppearInSerializedJson()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(ExampleHub));

        var json = GeneratorTestHelper.SerializeToJson(doc);
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(json);

        var mediaType = jsonDoc.RootElement
            .GetProperty("paths")
            .GetProperty(CreateOrderPath)
            .GetProperty("post")
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json");

        Assert.IsTrue(mediaType.TryGetProperty("examples", out var examples));

        Assert.IsTrue(examples.TryGetProperty("Created", out var created));
        Assert.AreEqual("Successfully created order", created.GetProperty("summary").GetString());
        var createdValue = created.GetProperty("value");
        Assert.AreEqual("ORD-001", createdValue.GetProperty("orderId").GetString());
        Assert.AreEqual("Created", createdValue.GetProperty("status").GetString());

        Assert.IsTrue(examples.TryGetProperty("Pending", out var pending));
        Assert.AreEqual("Order pending approval", pending.GetProperty("summary").GetString());
        var pendingValue = pending.GetProperty("value");
        Assert.AreEqual("ORD-002", pendingValue.GetProperty("orderId").GetString());
        Assert.AreEqual("PendingApproval", pendingValue.GetProperty("status").GetString());
    }

    /// <summary>
    /// Verifies simple-type response examples survive serialization to JSON.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SimpleTypeResponseExamplesAppearInSerializedJson()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(ExampleHub));

        var json = GeneratorTestHelper.SerializeToJson(doc);
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(json);

        var mediaType = jsonDoc.RootElement
            .GetProperty("paths")
            .GetProperty(GreetPath)
            .GetProperty("post")
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json");

        Assert.IsTrue(mediaType.TryGetProperty("examples", out var examples));

        Assert.IsTrue(examples.TryGetProperty("Casual", out var casual));
        Assert.AreEqual("Casual greeting", casual.GetProperty("summary").GetString());
        Assert.AreEqual("Hello, World!", casual.GetProperty("value").GetString());

        Assert.IsTrue(examples.TryGetProperty("Formal", out var formal));
        Assert.AreEqual("Formal greeting", formal.GetProperty("summary").GetString());
        Assert.AreEqual("Hello, Dr. Smith!", formal.GetProperty("value").GetString());
    }

    /// <summary>
    /// Verifies form-urlencoded schema properties get example values taken from the
    /// first provided example.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_FormSchemaHasPropertyExamplesFromProvider()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(ExampleHub));

        var op = doc.Paths[CreateOrderPath].Operations[OperationType.Post];

        Assert.IsTrue(
            op.RequestBody!.Content.ContainsKey("application/x-www-form-urlencoded"),
            "CreateOrder takes a single flat object, so it should offer form-urlencoded.");

        var formSchema = op.RequestBody.Content["application/x-www-form-urlencoded"].Schema;
        Assert.IsNotNull(formSchema.Properties);
        Assert.IsNotNull(formSchema.Properties["product"].Example);
        Assert.IsNotNull(formSchema.Properties["quantity"].Example);
        Assert.IsNotNull(formSchema.Properties["email"].Example);
    }

    /// <summary>
    /// Verifies example provider types are captured in the discovered method metadata.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ReadsExampleProviderAttributes()
    {
        var hubs = GeneratorTestHelper.DiscoverFor(typeof(ExampleHub));

        var exampleHub = hubs.First(h => h.Name == "Example");
        var createOrder = exampleHub.Methods.First(m => m.Name == "CreateOrder");

        Assert.AreEqual(1, createOrder.RequestExampleProviderTypes.Count);
        Assert.AreEqual(1, createOrder.ResponseExampleProviderTypes.Count);
        Assert.AreEqual(typeof(OrderRequestExamplesProvider), createOrder.RequestExampleProviderTypes[0]);
        Assert.AreEqual(typeof(OrderResponseExamplesProvider), createOrder.ResponseExampleProviderTypes[0]);
    }
}
