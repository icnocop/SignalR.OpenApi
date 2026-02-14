// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Examples;

/// <summary>
/// Provides one or more named examples of type <typeparamref name="T"/>
/// for OpenAPI documentation of SignalR hub methods.
/// </summary>
/// <typeparam name="T">The type of the example values.</typeparam>
public interface ISignalROpenApiExamplesProvider<T>
{
    /// <summary>
    /// Gets the examples to include in the OpenAPI specification.
    /// Return a single example for simple cases, or multiple for named examples.
    /// </summary>
    /// <returns>One or more named examples.</returns>
    IEnumerable<SignalROpenApiExample<T>> GetExamples();
}
