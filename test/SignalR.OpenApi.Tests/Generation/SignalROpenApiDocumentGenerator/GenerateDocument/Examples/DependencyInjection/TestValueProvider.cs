// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Examples.DependencyInjection;

/// <summary>
/// A test implementation of <see cref="ITestValueProvider"/> returning a fixed value.
/// </summary>
public sealed class TestValueProvider : ITestValueProvider
{
    private readonly string value;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestValueProvider"/> class.
    /// </summary>
    /// <param name="value">The value to return.</param>
    public TestValueProvider(string value)
    {
        this.value = value;
    }

    /// <inheritdoc/>
    public string GetValue() => this.value;
}
