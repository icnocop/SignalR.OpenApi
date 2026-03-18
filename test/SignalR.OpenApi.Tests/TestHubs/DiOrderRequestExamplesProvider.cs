// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// An examples provider that requires a dependency injected via the constructor.
/// </summary>
public class DiOrderRequestExamplesProvider : ISignalROpenApiExamplesProvider<DiRequest>
{
    private readonly ITestValueProvider valueProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiOrderRequestExamplesProvider"/> class.
    /// </summary>
    /// <param name="valueProvider">The test value provider.</param>
    public DiOrderRequestExamplesProvider(ITestValueProvider valueProvider)
    {
        this.valueProvider = valueProvider;
    }

    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<DiRequest>> GetExamples()
    {
        yield return new SignalROpenApiExample<DiRequest>(
            "DiExample",
            new DiRequest { Name = this.valueProvider.GetValue() })
        {
            Summary = "Example from DI provider",
        };
    }
}
