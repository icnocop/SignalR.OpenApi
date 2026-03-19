// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
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
    /// Verifies client event x-signalr extension includes parameterNames.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ClientEvent_IncludesParameterNames()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var eventOp = doc.Paths["/hubs/TypedChat/events/ReceiveMessage"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Get];

        var ext = (Microsoft.OpenApi.Any.OpenApiObject)eventOp.Extensions["x-signalr"];
        var paramNames = (Microsoft.OpenApi.Any.OpenApiArray)ext["parameterNames"];

        Assert.AreEqual(2, paramNames.Count, "ReceiveMessage should have 2 parameter names.");
        Assert.AreEqual("user", ((Microsoft.OpenApi.Any.OpenApiString)paramNames[0]).Value);
        Assert.AreEqual("message", ((Microsoft.OpenApi.Any.OpenApiString)paramNames[1]).Value);
    }

    /// <summary>
    /// Verifies polymorphic client event parameters include eventDiscriminators metadata.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ClientEvent_IncludesEventDiscriminators()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var eventOp = doc.Paths["/hubs/TypedChat/events/ShapeDrawn"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Get];

        var ext = (Microsoft.OpenApi.Any.OpenApiObject)eventOp.Extensions["x-signalr"];
        Assert.IsTrue(ext.ContainsKey("eventDiscriminators"), "Should have eventDiscriminators.");

        var discriminators = (Microsoft.OpenApi.Any.OpenApiObject)ext["eventDiscriminators"];
        Assert.IsTrue(discriminators.ContainsKey("shape"), "Should have discriminator for 'shape' parameter.");

        var shapeDisc = (Microsoft.OpenApi.Any.OpenApiObject)discriminators["shape"];
        Assert.AreEqual("kind", ((Microsoft.OpenApi.Any.OpenApiString)shapeDisc["property"]).Value);

        var mapping = (Microsoft.OpenApi.Any.OpenApiObject)shapeDisc["mapping"];
        Assert.IsTrue(mapping.ContainsKey("circle"), "Should have mapping for 'circle'.");
        Assert.IsTrue(mapping.ContainsKey("rectangle"), "Should have mapping for 'rectangle'.");
    }

    /// <summary>
    /// Verifies security schemes are added when authorized hubs exist and schemes are configured.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AddsSecuritySchemes_WhenAuthorizedHubExistsAndSchemesConfigured()
    {
        var (discoverer, generator) = CreateServices(o =>
        {
            o.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Bearer token.",
            };
        });

        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsNotNull(doc.Components.SecuritySchemes);
        Assert.IsTrue(doc.Components.SecuritySchemes.ContainsKey("Bearer"));
        Assert.AreEqual(SecuritySchemeType.Http, doc.Components.SecuritySchemes["Bearer"].Type);
    }

    /// <summary>
    /// Verifies no security schemes are added when authorized hubs exist but no schemes are configured.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_NoSecuritySchemes_WhenNoneConfigured()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        if (doc.Components.SecuritySchemes is not null)
        {
            foreach (var scheme in doc.Components.SecuritySchemes.Values)
            {
                Assert.AreNotEqual(
                    SecuritySchemeType.Http,
                    scheme.Type,
                    "Should not have HTTP auth schemes when SecuritySchemes is empty.");
            }
        }
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
        var (discoverer, generator) = CreateServices(o =>
        {
            o.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
            };
        });

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

    /// <summary>
    /// Verifies that discriminator property is visible in JSON schema
    /// when IncludeDiscriminatorInExamples is true (default).
    /// </summary>
    [TestMethod]
    public void GenerateDocument_IncludeDiscriminatorInExamples_DiscriminatorVisibleInJson()
    {
        var (discoverer, generator) = CreateServices(o => o.IncludeDiscriminatorInExamples = true);
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var circleOp = doc.Paths["/hubs/Polymorphic/DrawShape/circle"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var jsonSchema = circleOp.RequestBody.Content["application/json"].Schema;
        Assert.IsTrue(jsonSchema.Properties.ContainsKey("kind"), "JSON schema should contain discriminator.");
        Assert.IsFalse(jsonSchema.Properties["kind"].ReadOnly, "Discriminator should not be read-only in JSON schema.");
    }

    /// <summary>
    /// Verifies that discriminator property is hidden in form-urlencoded schema
    /// even when IncludeDiscriminatorInExamples is true.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_IncludeDiscriminatorInExamples_DiscriminatorHiddenInForm()
    {
        var (discoverer, generator) = CreateServices(o => o.IncludeDiscriminatorInExamples = true);
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var circleOp = doc.Paths["/hubs/Polymorphic/DrawShape/circle"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var formSchema = circleOp.RequestBody.Content["application/x-www-form-urlencoded"].Schema;
        Assert.IsTrue(formSchema.Properties.ContainsKey("kind"), "Form schema should contain discriminator.");
        Assert.IsTrue(formSchema.Properties["kind"].ReadOnly, "Discriminator should be read-only in form schema.");
    }

    /// <summary>
    /// Verifies that discriminator property is read-only in both schemas
    /// when IncludeDiscriminatorInExamples is false.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ExcludeDiscriminatorFromExamples_DiscriminatorHidden()
    {
        var (discoverer, generator) = CreateServices(o => o.IncludeDiscriminatorInExamples = false);
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var circleOp = doc.Paths["/hubs/Polymorphic/DrawShape/circle"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var jsonSchema = circleOp.RequestBody.Content["application/json"].Schema;
        Assert.IsTrue(jsonSchema.Properties["kind"].ReadOnly, "Discriminator should be read-only when excluded from examples.");
    }

    /// <summary>
    /// Verifies that polymorphic sub-endpoints include only examples matching the derived type.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_PolymorphicSubEndpoint_IncludesFilteredExamples()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // Main endpoint should have all examples.
        var mainOp = doc.Paths["/hubs/Polymorphic/DrawShape"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];
        var mainExamples = mainOp.RequestBody.Content["application/json"].Examples;
        Assert.IsNotNull(mainExamples, "Main endpoint should have examples.");
        Assert.AreEqual(2, mainExamples.Count, "Main endpoint should have both circle and rectangle examples.");

        // Circle sub-endpoint should only have the circle example.
        var circleOp = doc.Paths["/hubs/Polymorphic/DrawShape/circle"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];
        var circleExamples = circleOp.RequestBody.Content["application/json"].Examples;
        Assert.IsNotNull(circleExamples, "Circle sub-endpoint should have examples.");
        Assert.AreEqual(1, circleExamples.Count, "Circle sub-endpoint should have only circle example.");
        Assert.IsTrue(circleExamples.ContainsKey("SmallCircle"), "Circle sub-endpoint should contain SmallCircle example.");

        // Rectangle sub-endpoint should only have the rectangle example.
        var rectOp = doc.Paths["/hubs/Polymorphic/DrawShape/rectangle"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];
        var rectExamples = rectOp.RequestBody.Content["application/json"].Examples;
        Assert.IsNotNull(rectExamples, "Rectangle sub-endpoint should have examples.");
        Assert.AreEqual(1, rectExamples.Count, "Rectangle sub-endpoint should have only rectangle example.");
        Assert.IsTrue(rectExamples.ContainsKey("LargeRectangle"), "Rectangle sub-endpoint should contain LargeRectangle example.");
    }

    /// <summary>
    /// Verifies that form-urlencoded schema properties have example values
    /// set from the first example provider value.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_FormSchema_HasPropertyExamplesFromProvider()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var op = doc.Paths["/hubs/Example/CreateOrder"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsTrue(
            op.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"),
            "CreateOrder should have form-urlencoded content type.");

        var formSchema = op.RequestBody.Content["application/x-www-form-urlencoded"].Schema;
        Assert.IsNotNull(formSchema.Properties, "Form schema should have properties.");

        // First example from OrderRequestExamplesProvider: Product="Widget", Quantity=1, Email="alice@example.com"
        Assert.IsNotNull(formSchema.Properties["product"].Example, "Product property should have an example.");
        Assert.IsNotNull(formSchema.Properties["quantity"].Example, "Quantity property should have an example.");
        Assert.IsNotNull(formSchema.Properties["email"].Example, "Email property should have an example.");
    }

    /// <summary>
    /// Verifies that polymorphic sub-endpoint form schemas have property examples
    /// filtered by derived type, and that readOnly discriminator properties are skipped.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_PolymorphicSubEndpoint_FormSchemaHasFilteredPropertyExamples()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // Circle sub-endpoint form schema should have circle-specific examples.
        var circleOp = doc.Paths["/hubs/Polymorphic/DrawShape/circle"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];
        Assert.IsTrue(
            circleOp.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"),
            "Circle sub-endpoint should have form-urlencoded content type.");

        var circleFormSchema = circleOp.RequestBody.Content["application/x-www-form-urlencoded"].Schema;
        Assert.IsNotNull(circleFormSchema.Properties, "Circle form schema should have properties.");
        Assert.IsNotNull(circleFormSchema.Properties["color"].Example, "Color property should have an example.");
        Assert.IsNotNull(circleFormSchema.Properties["radius"].Example, "Radius property should have an example.");

        // Discriminator property should NOT have an example (readOnly).
        Assert.IsTrue(circleFormSchema.Properties["kind"].ReadOnly, "Discriminator should be readOnly.");
        Assert.IsNull(circleFormSchema.Properties["kind"].Example, "ReadOnly discriminator should not have an example.");
    }

    /// <summary>
    /// Verifies that examples providers with constructor dependencies are resolved via DI.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ExamplesProviderWithDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITestValueProvider>(new TestValueProvider("InjectedProduct"));
        using var serviceProvider = services.BuildServiceProvider();

        var options = new SignalROpenApiOptions
        {
            Assemblies = [typeof(DiExampleHub).Assembly],
        };

        var opts = Options.Create(options);
        var discoverer = new ReflectionHubDiscoverer(opts);
        var generator = new SignalROpenApiDocumentGenerator(opts, serviceProvider);

        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var submitRequest = doc.Paths["/hubs/DiExample/SubmitRequest"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsNotNull(submitRequest.RequestBody, "SubmitRequest should have a request body.");

        var jsonContent = submitRequest.RequestBody.Content["application/json"];
        Assert.IsNotNull(jsonContent.Examples, "Request body should have examples.");
        Assert.IsTrue(jsonContent.Examples.ContainsKey("DiExample"), "Should contain 'DiExample' example.");

        var diExample = jsonContent.Examples["DiExample"];
        Assert.AreEqual("Example from DI provider", diExample.Summary);

        // Verify the injected value was used
        Assert.IsNotNull(diExample.Value, "Example value should not be null.");
        var exampleObj = diExample.Value as Microsoft.OpenApi.Any.OpenApiObject;
        Assert.IsNotNull(exampleObj, "Example value should be an OpenApiObject.");
        var nameValue = exampleObj["name"] as Microsoft.OpenApi.Any.OpenApiString;
        Assert.IsNotNull(nameValue, "Name property should exist.");
        Assert.AreEqual("InjectedProduct", nameValue.Value, "Name should be the injected value.");
    }

    /// <summary>
    /// Verifies that enum schemas default to integer type with numeric values
    /// when no <see cref="JsonStringEnumConverter"/> is configured.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenNoJsonStringEnumConverterThenEnumSchemaIsInteger()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var getStatus = doc.Paths["/hubs/Enum/GetStatus"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var responseSchema = getStatus.Responses["200"].Content["application/json"].Schema;
        Assert.AreEqual("integer", responseSchema.Type, "Enum schema should be integer by default.");
        Assert.IsNotNull(responseSchema.Enum, "Enum should have values.");
        Assert.AreEqual(4, responseSchema.Enum.Count, "TestStatus has 4 values.");
        Assert.IsInstanceOfType(responseSchema.Enum[0], typeof(Microsoft.OpenApi.Any.OpenApiInteger));
    }

    /// <summary>
    /// Verifies that enum schemas use string type with enum names
    /// when <see cref="JsonStringEnumConverter"/> is configured in options.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenJsonStringEnumConverterConfiguredThenEnumSchemaIsString()
    {
        var (discoverer, generator) = CreateServices(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var getStatus = doc.Paths["/hubs/Enum/GetStatus"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        var responseSchema = getStatus.Responses["200"].Content["application/json"].Schema;
        Assert.AreEqual("string", responseSchema.Type, "Enum schema should be string with JsonStringEnumConverter.");
        Assert.IsNotNull(responseSchema.Enum, "Enum should have values.");
        Assert.AreEqual(4, responseSchema.Enum.Count, "TestStatus has 4 values.");
        Assert.IsInstanceOfType(responseSchema.Enum[0], typeof(Microsoft.OpenApi.Any.OpenApiString));

        var enumNames = responseSchema.Enum
            .Cast<Microsoft.OpenApi.Any.OpenApiString>()
            .Select(e => e.Value)
            .ToList();
        CollectionAssert.Contains(enumNames, "Pending");
        CollectionAssert.Contains(enumNames, "Active");
        CollectionAssert.Contains(enumNames, "Completed");
        CollectionAssert.Contains(enumNames, "Failed");
    }

    /// <summary>
    /// Verifies that enum property in a complex return type uses integer schema
    /// by default and string schema when <see cref="JsonStringEnumConverter"/> is configured.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenJsonStringEnumConverterConfiguredThenEnumPropertyInObjectIsString()
    {
        var (discoverer, generator) = CreateServices(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // TestResult is registered as a component schema because it's a complex object
        Assert.IsTrue(doc.Components.Schemas.ContainsKey("TestResult"), "TestResult should be in components/schemas.");
        var testResultSchema = doc.Components.Schemas["TestResult"];
        Assert.IsTrue(testResultSchema.Properties.ContainsKey("status"), "TestResult should have 'status' property.");
        var statusProp = testResultSchema.Properties["status"];
        Assert.AreEqual("string", statusProp.Type, "Status property should be string with JsonStringEnumConverter.");
    }

    /// <summary>
    /// Verifies the document-level tags list contains all unique tags from operations.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_PopulatesDocumentLevelTags()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsNotNull(doc.Tags, "Document should have tags.");
        Assert.IsTrue(doc.Tags.Count > 0, "Document should have at least one tag.");

        var tagNames = doc.Tags.Select(t => t.Name).ToList();

        // BasicHub has no [Tags] so its methods default to "Basic" tag
        CollectionAssert.Contains(tagNames, "Basic");

        // AttributeHub methods without [Tags] default to hub name "Attribute"
        CollectionAssert.Contains(tagNames, "Attribute");

        // GetUserDetailsAsync on AttributeHub uses [Tags("Users")]
        CollectionAssert.Contains(tagNames, "Users");
    }

    /// <summary>
    /// Verifies that tag descriptions configured via options are applied.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenTagDescriptionConfiguredThenApplied()
    {
        var (discoverer, generator) = CreateServices(o =>
        {
            o.TagDescriptions["Basic"] = "Basic hub operations";
        });

        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsNotNull(doc.Tags);
        var basicTag = doc.Tags.FirstOrDefault(t => t.Name == "Basic");
        Assert.IsNotNull(basicTag, "Document should contain a 'Basic' tag.");
        Assert.AreEqual("Basic hub operations", basicTag.Description);
    }

    /// <summary>
    /// Verifies that when no description is configured, the hub's XML summary
    /// is used as fallback when the tag name matches the hub name.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenNoTagDescriptionThenFallsBackToHubSummary()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsNotNull(doc.Tags);

        // BasicHub has XML summary "A basic hub for testing." and default tag "Basic"
        var basicTag = doc.Tags.FirstOrDefault(t => t.Name == "Basic");
        Assert.IsNotNull(basicTag, "Document should contain a 'Basic' tag.");
        Assert.AreEqual("A basic hub for testing.", basicTag.Description);
    }

    /// <summary>
    /// Verifies that configured tag descriptions take precedence over hub XML summaries.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenTagDescriptionConfiguredThenOverridesHubSummary()
    {
        var (discoverer, generator) = CreateServices(o =>
        {
            o.TagDescriptions["Basic"] = "Custom description";
        });

        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsNotNull(doc.Tags);
        var basicTag = doc.Tags.FirstOrDefault(t => t.Name == "Basic");
        Assert.IsNotNull(basicTag, "Document should contain a 'Basic' tag.");
        Assert.AreEqual("Custom description", basicTag.Description);
    }

    /// <summary>
    /// Verifies that tags from client events (e.g., "TypedChat Events") appear in document tags.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_IncludesClientEventTags()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsNotNull(doc.Tags);
        var tagNames = doc.Tags.Select(t => t.Name).ToList();
        CollectionAssert.Contains(tagNames, "TypedChat Events");
    }

    /// <summary>
    /// Verifies that document tags are deduplicated when multiple methods share the same tag.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_DeduplicatesDocumentTags()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsNotNull(doc.Tags);
        var tagNames = doc.Tags.Select(t => t.Name).ToList();
        var distinctNames = tagNames.Distinct().ToList();
        Assert.AreEqual(distinctNames.Count, tagNames.Count, "Document tags should be unique.");
    }

    /// <summary>
    /// Verifies that ApiKeyHeaders are emitted as apiKey security schemes in the document.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AddsApiKeyHeaderSchemes()
    {
        var (discoverer, generator) = CreateServices(o =>
        {
            o.ApiKeyHeaders["X-Custom-Header"] = "A custom header.";
            o.ApiKeyHeaders["X-Tenant-Id"] = "Tenant identifier.";
        });

        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        Assert.IsNotNull(doc.Components.SecuritySchemes);
        Assert.IsTrue(doc.Components.SecuritySchemes.ContainsKey("X-Custom-Header"));
        Assert.IsTrue(doc.Components.SecuritySchemes.ContainsKey("X-Tenant-Id"));

        var customHeader = doc.Components.SecuritySchemes["X-Custom-Header"];
        Assert.AreEqual(SecuritySchemeType.ApiKey, customHeader.Type);
        Assert.AreEqual(ParameterLocation.Header, customHeader.In);
        Assert.AreEqual("X-Custom-Header", customHeader.Name);
        Assert.AreEqual("A custom header.", customHeader.Description);
    }

    /// <summary>
    /// Verifies that no apiKey schemes are added when ApiKeyHeaders is empty.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_NoApiKeyHeaders_DoesNotAddApiKeySchemes()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        if (doc.Components.SecuritySchemes is not null)
        {
            foreach (var scheme in doc.Components.SecuritySchemes.Values)
            {
                Assert.AreNotEqual(SecuritySchemeType.ApiKey, scheme.Type, "Should not have apiKey schemes when ApiKeyHeaders is empty.");
            }
        }
    }

    /// <summary>
    /// Verifies that a method with a complex object and a string parameter generates
    /// a JSON-only wrapper schema with both parameters as named properties.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenObjectAndStringParametersThenWrappedJsonOnlySchema()
    {
        var (discoverer, generator) = CreateServices();
        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        // BasicHub.SubmitFeedbackWithNote(FeedbackMessage feedback, string note)
        var submitFeedback = doc.Paths["/hubs/Basic/SubmitFeedbackWithNote"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];

        Assert.IsNotNull(submitFeedback.RequestBody, "Should have a request body.");

        // Should have JSON content type only (no form-urlencoded for mixed object+primitive)
        Assert.IsTrue(submitFeedback.RequestBody.Content.ContainsKey("application/json"), "Should have application/json.");
        Assert.IsFalse(submitFeedback.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded"), "Mixed object+string should not have form-urlencoded.");

        var schema = submitFeedback.RequestBody.Content["application/json"].Schema;
        Assert.AreEqual("object", schema.Type, "Schema should be an object wrapper.");

        // Both parameters should appear as named properties
        Assert.IsTrue(schema.Properties.ContainsKey("feedback"), "Should contain 'feedback' property for the FeedbackMessage parameter.");
        Assert.IsTrue(schema.Properties.ContainsKey("note"), "Should contain 'note' property for the string parameter.");
        Assert.AreEqual(2, schema.Properties.Count, "Should have exactly 2 properties.");

        // The 'note' property should be a string schema
        Assert.AreEqual("string", schema.Properties["note"].Type, "The 'note' property should be a string type.");

        // x-signalr should have parameterCount=2 and flattenedBody=false
        var ext = (Microsoft.OpenApi.Any.OpenApiObject)submitFeedback.Extensions["x-signalr"];
        var paramCount = (Microsoft.OpenApi.Any.OpenApiInteger)ext["parameterCount"];
        Assert.AreEqual(2, paramCount.Value, "parameterCount should be 2.");
        var flattenedBody = (Microsoft.OpenApi.Any.OpenApiBoolean)ext["flattenedBody"];
        Assert.IsFalse(flattenedBody.Value, "flattenedBody should be false for mixed parameters.");
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

    private sealed class TestValueProvider : ITestValueProvider
    {
        private readonly string value;

        public TestValueProvider(string value)
        {
            this.value = value;
        }

        public string GetValue() => this.value;
    }
}
