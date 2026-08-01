// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignalR.OpenApi.Discovery;
using SignalR.OpenApi.Generation;
using SignalR.OpenApi.Models;

namespace SignalR.OpenApi.Tests.Generation;

/// <summary>
/// Shared helpers for <see cref="SignalROpenApiDocumentGenerator"/> tests.
/// </summary>
/// <remarks>
/// Tests declare the hub types they care about so each generated document contains
/// only those hubs. Without that, every test would see every hub in the test
/// assembly and be coupled to unrelated fixtures.
/// </remarks>
internal static class GeneratorTestHelper
{
    /// <summary>
    /// Discovers only the specified hub types.
    /// </summary>
    /// <param name="hubTypes">The hub types to include.</param>
    /// <returns>The discovered hubs.</returns>
    public static IReadOnlyList<SignalRHubInfo> DiscoverFor(params Type[] hubTypes)
    {
        var (discoverer, _) = CreateServices(null, hubTypes);
        return discoverer.DiscoverHubs();
    }

    /// <summary>
    /// Generates a document containing only the specified hub types.
    /// </summary>
    /// <param name="hubTypes">The hub types to include.</param>
    /// <returns>The generated document.</returns>
    public static OpenApiDocument GenerateFor(params Type[] hubTypes)
    {
        return GenerateFor(configure: null, hubTypes);
    }

    /// <summary>
    /// Generates a document containing only the specified hub types, with additional
    /// option configuration.
    /// </summary>
    /// <param name="configure">Additional option configuration, or <see langword="null"/>.</param>
    /// <param name="hubTypes">The hub types to include.</param>
    /// <returns>The generated document.</returns>
    public static OpenApiDocument GenerateFor(
        Action<SignalROpenApiOptions>? configure,
        params Type[] hubTypes)
    {
        var (discoverer, generator) = CreateServices(configure, hubTypes);
        return generator.GenerateDocument(discoverer.DiscoverHubs());
    }

    /// <summary>
    /// Generates a document containing only the specified hub types, resolving example
    /// providers from the supplied service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve example providers.</param>
    /// <param name="hubTypes">The hub types to include.</param>
    /// <returns>The generated document.</returns>
    public static OpenApiDocument GenerateWithServices(
        IServiceProvider serviceProvider,
        params Type[] hubTypes)
    {
        var options = CreateOptions(null, hubTypes);
        var discoverer = new ReflectionHubDiscoverer(options);
        var generator = new SignalROpenApiDocumentGenerator(options, serviceProvider);
        return generator.GenerateDocument(discoverer.DiscoverHubs());
    }

    /// <summary>
    /// Generates a document containing every hub in the test assembly. Use only for
    /// assertions about document-wide invariants.
    /// </summary>
    /// <param name="configure">Additional option configuration, or <see langword="null"/>.</param>
    /// <returns>The generated document.</returns>
    public static OpenApiDocument GenerateForAllHubs(Action<SignalROpenApiOptions>? configure = null)
    {
        var (discoverer, generator) = CreateServices(configure, null);
        return generator.GenerateDocument(discoverer.DiscoverHubs());
    }

    /// <summary>
    /// Serializes a document to OpenAPI v3 JSON.
    /// </summary>
    /// <param name="doc">The document to serialize.</param>
    /// <returns>The JSON representation.</returns>
    public static string SerializeToJson(OpenApiDocument doc)
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        var jsonWriter = new Microsoft.OpenApi.Writers.OpenApiJsonWriter(writer);
        doc.SerializeAsV3(jsonWriter);
        writer.Flush();

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Asserts that every schema reference in the document resolves to an entry in
    /// components/schemas, so the document contains no dangling <c>$ref</c>.
    /// </summary>
    /// <param name="doc">The document to check.</param>
    public static void AssertAllSchemaReferencesResolve(OpenApiDocument doc)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);

        void Visit(OpenApiSchema? schema, string path)
        {
            if (schema is null || !visited.Add(schema))
            {
                return;
            }

            if (schema.Reference is not null && schema.Reference.Type == ReferenceType.Schema)
            {
                Assert.IsTrue(
                    doc.Components.Schemas.ContainsKey(schema.Reference.Id),
                    $"Reference '{schema.Reference.Id}' at {path} does not resolve to a registered schema.");
            }

            Visit(schema.Items, $"{path}/items");
            Visit(schema.AdditionalProperties, $"{path}/additionalProperties");

            if (schema.Properties is not null)
            {
                foreach (var property in schema.Properties)
                {
                    Visit(property.Value, $"{path}/properties/{property.Key}");
                }
            }

            VisitAll(schema.OneOf, $"{path}/oneOf");
            VisitAll(schema.AnyOf, $"{path}/anyOf");
            VisitAll(schema.AllOf, $"{path}/allOf");
        }

        void VisitAll(IList<OpenApiSchema>? schemas, string path)
        {
            if (schemas is null)
            {
                return;
            }

            foreach (var schema in schemas)
            {
                Visit(schema, path);
            }
        }

        void VisitContent(IDictionary<string, OpenApiMediaType>? content, string path)
        {
            if (content is null)
            {
                return;
            }

            foreach (var mediaType in content)
            {
                Visit(mediaType.Value.Schema, $"{path}/{mediaType.Key}");
            }
        }

        foreach (var componentSchema in doc.Components.Schemas)
        {
            Visit(componentSchema.Value, $"components/schemas/{componentSchema.Key}");
        }

        foreach (var path in doc.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                VisitContent(operation.Value.RequestBody?.Content, $"{path.Key}/{operation.Key}/requestBody");

                foreach (var response in operation.Value.Responses)
                {
                    VisitContent(
                        response.Value.Content,
                        $"{path.Key}/{operation.Key}/responses/{response.Key}");
                }
            }
        }
    }

    private static IOptions<SignalROpenApiOptions> CreateOptions(
        Action<SignalROpenApiOptions>? configure,
        Type[]? hubTypes)
    {
        var options = new SignalROpenApiOptions
        {
            Assemblies = [typeof(GeneratorTestHelper).Assembly],
        };

        configure?.Invoke(options);

        if (hubTypes is not null)
        {
            var included = new HashSet<Type>(hubTypes);
            options.HubFilter = included.Contains;
        }

        return Options.Create(options);
    }

    private static (ReflectionHubDiscoverer Discoverer, SignalROpenApiDocumentGenerator Generator) CreateServices(
        Action<SignalROpenApiOptions>? configure,
        Type[]? hubTypes)
    {
        var options = CreateOptions(configure, hubTypes);
        return (
            new ReflectionHubDiscoverer(options),
            new SignalROpenApiDocumentGenerator(options, new EmptyServiceProvider()));
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
