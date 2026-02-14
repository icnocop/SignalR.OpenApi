// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SignalR.OpenApi.FluentValidation;

namespace SignalR.OpenApi.Extensions;

/// <summary>
/// Extension methods for adding SignalR OpenAPI FluentValidation integration to the dependency injection container.
/// </summary>
public static class FluentValidationServiceCollectionExtensions
{
    /// <summary>
    /// Adds FluentValidation schema processing to SignalR OpenAPI document generation.
    /// FluentValidation rules registered in the DI container are automatically mapped
    /// to OpenAPI schema constraints (required, minLength, maxLength, pattern, minimum, maximum).
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method should be called after <c>AddSignalROpenApi()</c>. FluentValidation
    /// validators must be registered in the DI container (e.g., via
    /// <c>AddValidatorsFromAssemblyContaining</c>) for rules to be applied.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddSignalRFluentValidation(this IServiceCollection services)
    {
        services.TryAddSingleton<ISignalROpenApiSchemaProcessor, FluentValidationSchemaProcessor>();

        return services;
    }
}
