// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Examples;

/// <summary>
/// Specifies an examples provider for the request body of a SignalR hub method.
/// Multiple instances of this attribute can be applied to the same method to provide
/// examples for different request types.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SignalROpenApiRequestExamplesAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignalROpenApiRequestExamplesAttribute"/> class.
    /// </summary>
    /// <param name="examplesProviderType">
    /// The type that implements <see cref="ISignalROpenApiExamplesProvider{T}"/>.
    /// Must have a parameterless constructor or be registered in the DI container.
    /// </param>
    public SignalROpenApiRequestExamplesAttribute(Type examplesProviderType)
    {
        this.ExamplesProviderType = examplesProviderType;
    }

    /// <summary>
    /// Gets the type that implements <see cref="ISignalROpenApiExamplesProvider{T}"/>.
    /// </summary>
    public Type ExamplesProviderType { get; }
}
