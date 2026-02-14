// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;

namespace SignalR.OpenApi;

/// <summary>
/// Provides a mechanism to post-process OpenAPI schemas after they are generated.
/// Implementations can modify schema properties such as adding validation constraints,
/// custom extensions, or other schema-level modifications.
/// </summary>
public interface ISignalROpenApiSchemaProcessor
{
    /// <summary>
    /// Processes the specified schema for the given CLR type.
    /// Called after the schema is created from type metadata and data annotations.
    /// </summary>
    /// <param name="schema">The OpenAPI schema to process.</param>
    /// <param name="type">The CLR type the schema was generated from.</param>
    void ProcessSchema(OpenApiSchema schema, Type type);
}
