// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Examples.DependencyInjection;

/// <summary>
/// A dependency used to verify DI support in examples providers.
/// </summary>
public interface ITestValueProvider
{
    /// <summary>
    /// Gets a value used in examples.
    /// </summary>
    /// <returns>A test value.</returns>
    string GetValue();
}
