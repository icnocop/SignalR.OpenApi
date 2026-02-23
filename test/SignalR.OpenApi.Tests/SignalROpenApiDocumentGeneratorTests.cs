// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignalR.OpenApi.Discovery;
using SignalR.OpenApi.Generation;
using SignalR.OpenApi.Tests.TestHubs;

namespace SignalR.OpenApi.Tests;

/// <summary>
/// Tests for <see cref="SignalROpenApiDocumentGenerator"/>.
/// </summary>
[TestClass]
public class SignalROpenApiDocumentGeneratorTests
{
    /// <summary>
    /// Verifies the document has correct info metadata.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_HasCorrectInfoMetadata()
    {
        var (discoverer, generator) = CreateServices(o =>
        {
            o.DocumentTitle = "Test API";
            o.DocumentVersion = "v2";
        });

        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.AreEqual("Test API", doc.Info.Title);
        Assert.AreEqual("v2", doc.Info.Version);
    }

    /// <summary>
    /// Verifies paths are created for hub methods.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_CreatesPathsForHubMethods()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsTrue(doc.Paths.ContainsKey("/hubs/Basic/SendMessage"));
        Assert.IsTrue(doc.Paths.ContainsKey("/hubs/Basic/GetTime"));
    }

    /// <summary>
    /// Verifies all hub methods use POST operations.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_UsesPostForHubMethods()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        foreach (var path in doc.Paths)
        {
            if (path.Key.Contains("/events/", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.IsTrue(
                path.Value.Operations.ContainsKey(Microsoft.OpenApi.Models.OperationType.Post),
                $"Path {path.Key} should use POST");
        }
    }

    /// <summary>
    /// Verifies x-signalr extension is added to operations.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AddsSignalRExtension()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var sendMessage = doc.Paths["/hubs/Basic/SendMessage"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsTrue(sendMessage.Extensions.ContainsKey("x-signalr"));
    }

    /// <summary>
    /// Verifies streaming methods have x-signalr stream flag set to true.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_StreamingMethodsHaveStreamFlag()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var streamPath = doc.Paths["/hubs/Streaming/StreamIntegers"];
        var operation = streamPath.Operations[Microsoft.OpenApi.Models.OperationType.Post];
        var extension = (Microsoft.OpenApi.Any.OpenApiObject)operation.Extensions["x-signalr"];
        var streamFlag = (Microsoft.OpenApi.Any.OpenApiBoolean)extension["stream"];

        Assert.IsTrue(streamFlag.Value);
    }

    /// <summary>
    /// Verifies client events are created as GET operations.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ClientEventsAsGetOperations()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsTrue(doc.Paths.ContainsKey("/hubs/TypedChat/events/ReceiveMessage"));

        var eventPath = doc.Paths["/hubs/TypedChat/events/ReceiveMessage"];
        Assert.IsTrue(eventPath.Operations.ContainsKey(Microsoft.OpenApi.Models.OperationType.Get));
    }

    /// <summary>
    /// Verifies security schemes are added when authorized hubs exist.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AddsSecuritySchemes_WhenAuthorizedHubExists()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsNotNull(doc.Components.SecuritySchemes);
        Assert.IsTrue(doc.Components.SecuritySchemes.ContainsKey("Bearer"));
    }

    /// <summary>
    /// Verifies deprecated methods are marked in the document.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_MarksDeprecatedMethods()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var legacyPath = doc.Paths["/hubs/Attribute/GetUserLegacy"];
        var operation = legacyPath.Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsTrue(operation.Deprecated);
    }

    /// <summary>
    /// Verifies request body schema is generated for methods with parameters.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_GeneratesRequestBodySchema()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var sendMessage = doc.Paths["/hubs/Basic/SendMessage"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsNotNull(sendMessage.RequestBody);
        Assert.IsTrue(sendMessage.RequestBody.Content.ContainsKey("application/json"));

        var schema = sendMessage.RequestBody.Content["application/json"].Schema;
        Assert.AreEqual("object", schema.Type);
        Assert.IsTrue(schema.Properties.ContainsKey("message"));
    }

    /// <summary>
    /// Verifies response schema is generated for methods with return types.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_GeneratesResponseSchema()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var sendMessage = doc.Paths["/hubs/Basic/SendMessage"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsTrue(sendMessage.Responses.ContainsKey("200"));
        var response = sendMessage.Responses["200"];
        Assert.IsTrue(response.Content.ContainsKey("application/json"));
        Assert.AreEqual("string", response.Content["application/json"].Schema.Type);
    }

    /// <summary>
    /// Verifies security requirements are set on authorized methods.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SetsSecurityOnAuthorizedMethods()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // AttributeHub is [Authorize], so its non-AllowAnonymous methods should have security
        var getUser = doc.Paths["/hubs/Attribute/GetUserDetails"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsNotNull(getUser.Security);
        Assert.IsTrue(getUser.Security.Count > 0);
    }

    /// <summary>
    /// Verifies data annotations are applied to schemas.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AppliesDataAnnotationsToSchema()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var validateInput = doc.Paths["/hubs/Attribute/ValidateInput"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var requestSchema = validateInput.RequestBody!.Content["application/json"].Schema;
        Assert.AreEqual("object", requestSchema.Type);

        // Single complex object parameter is flattened — properties are at the root level
        Assert.IsTrue(requestSchema.Properties.ContainsKey("name"), "Should contain 'name' property from ValidatedModel.");
        Assert.IsTrue(requestSchema.Properties.ContainsKey("email"), "Should contain 'email' property from ValidatedModel.");
        Assert.IsTrue(requestSchema.Properties.ContainsKey("age"), "Should contain 'age' property from ValidatedModel.");
    }

    /// <summary>
    /// Verifies all operations have valid operation IDs.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AllOperationsHaveOperationIds()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        foreach (var path in doc.Paths)
        {
            foreach (var op in path.Value.Operations)
            {
                Assert.IsFalse(
                    string.IsNullOrEmpty(op.Value.OperationId),
                    $"Path {path.Key} operation {op.Key} should have an operationId");
            }
        }
    }

    /// <summary>
    /// Verifies all responses have descriptions.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AllResponsesHaveDescriptions()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        foreach (var path in doc.Paths)
        {
            foreach (var op in path.Value.Operations)
            {
                foreach (var resp in op.Value.Responses)
                {
                    Assert.IsFalse(
                        string.IsNullOrEmpty(resp.Value.Description),
                        $"Path {path.Key} response {resp.Key} should have a description");
                }
            }
        }
    }

    /// <summary>
    /// Verifies the document can be serialized to JSON without errors.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SerializesToValidJson()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        var jsonWriter = new Microsoft.OpenApi.Writers.OpenApiJsonWriter(writer);
        doc.SerializeAsV3(jsonWriter);
        writer.Flush();

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        Assert.IsFalse(string.IsNullOrEmpty(json));
        Assert.IsTrue(json.Contains("\"openapi\""));
        Assert.IsTrue(json.Contains("\"paths\""));
    }

    /// <summary>
    /// Verifies that request example attributes are read from hub methods.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_RequestExamplesFromProvider()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var createOrder = doc.Paths["/hubs/Example/CreateOrder"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsNotNull(createOrder.RequestBody, "CreateOrder should have a request body.");

        var jsonContent = createOrder.RequestBody.Content["application/json"];
        Assert.IsNotNull(jsonContent.Examples, "Request body should have examples.");
        Assert.IsTrue(jsonContent.Examples.ContainsKey("SingleItem"), "Should contain 'SingleItem' example.");
        Assert.IsTrue(jsonContent.Examples.ContainsKey("BulkOrder"), "Should contain 'BulkOrder' example.");

        var singleItem = jsonContent.Examples["SingleItem"];
        Assert.AreEqual("Single item order", singleItem.Summary);
    }

    /// <summary>
    /// Verifies that response example attributes are read from hub methods.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ResponseExamplesFromProvider()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var createOrder = doc.Paths["/hubs/Example/CreateOrder"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var response = createOrder.Responses["200"];
        Assert.IsNotNull(response.Content, "Response should have content.");
        var jsonContent = response.Content["application/json"];
        Assert.IsNotNull(jsonContent.Examples, "Response should have examples.");
        Assert.IsTrue(jsonContent.Examples.ContainsKey("Created"), "Should contain 'Created' example.");
        Assert.IsTrue(jsonContent.Examples.ContainsKey("Pending"), "Should contain 'Pending' example.");
    }

    /// <summary>
    /// Verifies that simple type response examples work (e.g., string).
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SimpleTypeResponseExamples()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var greet = doc.Paths["/hubs/Example/Greet"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var response = greet.Responses["200"];
        var jsonContent = response.Content["application/json"];
        Assert.IsNotNull(jsonContent.Examples, "Greet response should have examples.");
        Assert.IsTrue(jsonContent.Examples.ContainsKey("Casual"), "Should contain 'Casual' example.");
        Assert.IsTrue(jsonContent.Examples.ContainsKey("Formal"), "Should contain 'Formal' example.");

        var casual = jsonContent.Examples["Casual"];
        Assert.AreEqual("Casual greeting", casual.Summary);
    }

    /// <summary>
    /// Verifies that example provider types are discovered in method metadata.
    /// </summary>
    [TestMethod]
    public void DiscoverHubs_ReadsExampleProviderAttributes()
    {
        var (discoverer, _) = CreateServices();
        var hubs = discoverer.DiscoverHubs();

        var exampleHub = hubs.First(h => h.Name == "Example");
        var createOrder = exampleHub.Methods.First(m => m.Name == "CreateOrder");

        Assert.AreEqual(1, createOrder.RequestExampleProviderTypes.Count, "Should have 1 request example provider.");
        Assert.AreEqual(1, createOrder.ResponseExampleProviderTypes.Count, "Should have 1 response example provider.");
        Assert.AreEqual(typeof(TestHubs.OrderRequestExamplesProvider), createOrder.RequestExampleProviderTypes[0]);
        Assert.AreEqual(typeof(TestHubs.OrderResponseExamplesProvider), createOrder.ResponseExampleProviderTypes[0]);
    }

    /// <summary>
    /// Verifies that a single complex object parameter is flattened in the request body schema.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SingleObjectParam_FlattenedSchema()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // ExampleHub.CreateOrder(OrderRequest order) has one complex object parameter
        var createOrder = doc.Paths["/hubs/Example/CreateOrder"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var schema = createOrder.RequestBody!.Content["application/json"].Schema;
        Assert.AreEqual("object", schema.Type);

        // Properties should be flattened — no "order" wrapper
        Assert.IsFalse(schema.Properties.ContainsKey("order"), "Should not have a wrapper 'order' property.");
        Assert.IsTrue(schema.Properties.ContainsKey("product"), "Should contain 'product' property from OrderRequest.");
        Assert.IsTrue(schema.Properties.ContainsKey("quantity"), "Should contain 'quantity' property from OrderRequest.");
    }

    /// <summary>
    /// Verifies that multiple parameters generate a wrapper schema with each parameter as a property.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_MultipleParams_WrappedSchema()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // BasicHub.SendMessage(string message) has one primitive parameter — always wrapped
        var sendMessage = doc.Paths["/hubs/Basic/SendMessage"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var schema = sendMessage.RequestBody!.Content["application/json"].Schema;
        Assert.AreEqual("object", schema.Type);
        Assert.IsTrue(schema.Properties.ContainsKey("message"), "Should contain 'message' property.");
    }

    /// <summary>
    /// Verifies that parameterCount is included in the x-signalr extension.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ParameterCountInExtension()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // BasicHub.SendMessage(string message) — 1 parameter
        var sendMessage = doc.Paths["/hubs/Basic/SendMessage"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];
        var ext1 = (Microsoft.OpenApi.Any.OpenApiObject)sendMessage.Extensions["x-signalr"];
        var count1 = (Microsoft.OpenApi.Any.OpenApiInteger)ext1["parameterCount"];
        Assert.AreEqual(1, count1.Value);

        // BasicHub.GetTime() — 0 parameters
        var getTime = doc.Paths["/hubs/Basic/GetTime"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];
        var ext2 = (Microsoft.OpenApi.Any.OpenApiObject)getTime.Extensions["x-signalr"];
        var count2 = (Microsoft.OpenApi.Any.OpenApiInteger)ext2["parameterCount"];
        Assert.AreEqual(0, count2.Value);
    }

    /// <summary>
    /// Verifies that void methods (Task with no return value) return 204 No Content.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_VoidMethod_Returns204()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // TypedChatHub.SendMessage(string, string) returns Task (no return value)
        var sendMessage = doc.Paths["/hubs/TypedChat/SendMessage"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsTrue(sendMessage.Responses.ContainsKey("204"), "Void methods should return 204.");
        Assert.IsFalse(sendMessage.Responses.ContainsKey("200"), "Void methods should not return 200.");
        Assert.IsNotNull(sendMessage.Responses["204"].Description);
    }

    /// <summary>
    /// Verifies that methods with return types still return 200 OK.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ReturnTypeMethod_Returns200()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // BasicHub.SendMessage(string) returns Task<string>
        var sendMessage = doc.Paths["/hubs/Basic/SendMessage"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsTrue(sendMessage.Responses.ContainsKey("200"), "Methods with return types should return 200.");
        Assert.IsFalse(sendMessage.Responses.ContainsKey("204"), "Methods with return types should not return 204.");
    }

    /// <summary>
    /// Verifies that request body includes both application/json and application/x-www-form-urlencoded content types.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_RequestBodyHasBothContentTypes()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var sendMessage = doc.Paths["/hubs/Basic/SendMessage"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsNotNull(sendMessage.RequestBody);
        Assert.IsTrue(sendMessage.RequestBody.Content.ContainsKey("application/json"), "Should have application/json content type.");
        Assert.IsTrue(sendMessage.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"), "Should have application/x-www-form-urlencoded content type.");

        // Both content types should share the same schema
        var jsonSchema = sendMessage.RequestBody.Content["application/json"].Schema;
        var formSchema = sendMessage.RequestBody.Content["application/x-www-form-urlencoded"].Schema;
        Assert.AreEqual(jsonSchema.Type, formSchema.Type);
        Assert.AreEqual(jsonSchema.Properties.Count, formSchema.Properties.Count);
    }

    /// <summary>
    /// Verifies that polymorphic types generate oneOf with discriminator in the schema.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_PolymorphicParameter_GeneratesOneOfWithDiscriminator()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // PolymorphicHub.DrawShape(TestShape shape) — TestShape has [JsonPolymorphic] + [JsonDerivedType]
        var drawShape = doc.Paths["/hubs/Polymorphic/DrawShape"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsNotNull(drawShape.RequestBody);
        var schema = drawShape.RequestBody.Content["application/json"].Schema;

        // Single complex object parameter — schema should be flattened (the TestShape schema directly)
        Assert.IsNotNull(schema.OneOf, "Polymorphic schema should have OneOf.");
        Assert.AreEqual(2, schema.OneOf.Count, "Should have 2 derived types (circle, rectangle).");
        Assert.IsNotNull(schema.Discriminator, "Should have a discriminator.");
        Assert.AreEqual("kind", schema.Discriminator.PropertyName, "Discriminator property should be 'kind'.");

        // Verify $ref schemas are registered in components
        Assert.IsTrue(doc.Components.Schemas.ContainsKey("CircleShape"), "CircleShape should be in components/schemas.");
        Assert.IsTrue(doc.Components.Schemas.ContainsKey("RectangleShape"), "RectangleShape should be in components/schemas.");

        // Verify discriminator mapping
        Assert.IsNotNull(schema.Discriminator.Mapping, "Discriminator should have mapping.");
        Assert.AreEqual("#/components/schemas/CircleShape", schema.Discriminator.Mapping["circle"]);
        Assert.AreEqual("#/components/schemas/RectangleShape", schema.Discriminator.Mapping["rectangle"]);
    }

    /// <summary>
    /// Verifies that flat object parameters have both application/json and form-urlencoded content types.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_FlatObjectParameter_HasFormUrlEncoded()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // AttributeHub.ValidateInput(ValidatedModel input) — single flat object
        var validateInput = doc.Paths["/hubs/Attribute/ValidateInput"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsTrue(validateInput.RequestBody.Content.ContainsKey("application/json"), "Should have application/json.");
        Assert.IsTrue(validateInput.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"), "Flat object should have form-urlencoded.");
    }

    /// <summary>
    /// Verifies that polymorphic parameters generate sub-endpoints per derived type.
    /// The main endpoint stays JSON-only with oneOf, while sub-endpoints are flat and form-friendly.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_PolymorphicParameter_GeneratesSubEndpoints()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // Main endpoint should remain JSON-only with oneOf schema
        var drawShape = doc.Paths["/hubs/Polymorphic/DrawShape"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];
        Assert.IsTrue(drawShape.RequestBody.Content.ContainsKey("application/json"), "Main endpoint should have application/json.");
        Assert.IsFalse(drawShape.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"), "Main polymorphic endpoint should not have form-urlencoded.");

        // Sub-endpoints for each derived type
        Assert.IsTrue(doc.Paths.ContainsKey("/hubs/Polymorphic/DrawShape/circle"), "Should have circle sub-endpoint.");
        Assert.IsTrue(doc.Paths.ContainsKey("/hubs/Polymorphic/DrawShape/rectangle"), "Should have rectangle sub-endpoint.");

        var circleOp = doc.Paths["/hubs/Polymorphic/DrawShape/circle"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];
        Assert.IsTrue(circleOp.RequestBody.Content.ContainsKey("application/json"), "Circle sub-endpoint should have JSON.");
        Assert.IsTrue(circleOp.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"), "Circle sub-endpoint should have form-urlencoded.");

        // Verify discriminator metadata in x-signalr extension
        var circleExt = (Microsoft.OpenApi.Any.OpenApiObject)circleOp.Extensions["x-signalr"];
        Assert.AreEqual("DrawShape", ((Microsoft.OpenApi.Any.OpenApiString)circleExt["method"]).Value, "Sub-endpoint method should be the hub method name.");
        Assert.AreEqual("kind", ((Microsoft.OpenApi.Any.OpenApiString)circleExt["discriminatorProperty"]).Value, "Should have discriminator property.");
        Assert.AreEqual("circle", ((Microsoft.OpenApi.Any.OpenApiString)circleExt["discriminatorValue"]).Value, "Should have discriminator value.");
    }

    /// <summary>
    /// Verifies that multiple complex object parameters are JSON-only (no form-urlencoded).
    /// </summary>
    [TestMethod]
    public void GenerateDocument_MultiObjectParameters_JsonOnly()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // AttributeHub.MultiObjectInput(ValidatedModel first, ValidatedModel second)
        var multiObject = doc.Paths["/hubs/Attribute/MultiObjectInput"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsTrue(multiObject.RequestBody.Content.ContainsKey("application/json"), "Should have application/json.");
        Assert.IsFalse(multiObject.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"), "Multiple complex object parameters should not have form-urlencoded.");
    }

    /// <summary>
    /// Verifies that flattenedBody is true for single complex object parameter.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_FlattenedBody_TrueForSingleObjectParameter()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // AttributeHub.ValidateInput(ValidatedModel input) — single complex object
        var validateInput = doc.Paths["/hubs/Attribute/ValidateInput"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var ext = (Microsoft.OpenApi.Any.OpenApiObject)validateInput.Extensions["x-signalr"];
        var flattenedBody = ((Microsoft.OpenApi.Any.OpenApiBoolean)ext["flattenedBody"]).Value;
        Assert.IsTrue(flattenedBody, "flattenedBody should be true for single complex object parameter.");
    }

    /// <summary>
    /// Verifies that flattenedBody is false for multiple or primitive parameters.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_FlattenedBody_FalseForPrimitiveParameters()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // BasicHub.SendMessage(string message) — single primitive
        var sendMessage = doc.Paths["/hubs/Basic/SendMessage"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var ext = (Microsoft.OpenApi.Any.OpenApiObject)sendMessage.Extensions["x-signalr"];
        var flattenedBody = ((Microsoft.OpenApi.Any.OpenApiBoolean)ext["flattenedBody"]).Value;
        Assert.IsFalse(flattenedBody, "flattenedBody should be false for primitive parameters.");
    }

    /// <summary>
    /// Verifies that self-referencing types do not cause a StackOverflowException.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SelfReferencingType_DoesNotStackOverflow()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsTrue(doc.Paths.ContainsKey("/hubs/CircularRef/ProcessNode"));
        Assert.IsTrue(doc.Components.Schemas.ContainsKey("TreeNode"));
    }

    private static (ReflectionHubDiscoverer Discoverer, SignalROpenApiDocumentGenerator Generator) CreateServices(
        Action<SignalROpenApiOptions>? configure = null)
    {
        var options = new SignalROpenApiOptions
        {
            Assemblies = [typeof(BasicHub).Assembly],
        };

        configure?.Invoke(options);

        var opts = Options.Create(options);
        var serviceProvider = new EmptyServiceProvider();
        return (new ReflectionHubDiscoverer(opts), new SignalROpenApiDocumentGenerator(opts, serviceProvider));
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
