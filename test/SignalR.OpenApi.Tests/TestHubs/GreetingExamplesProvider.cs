// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// Provides response examples for the Greet method.
/// </summary>
public class GreetingExamplesProvider : ISignalROpenApiExamplesProvider<string>
{
    /// <inheritdoc/>
    public IEnumerable<SignalROpenApiExample<string>> GetExamples()
    {
        yield return new SignalROpenApiExample<string>("Casual", "Hello, World!")
        {
            Summary = "Casual greeting",
        };

        yield return new SignalROpenApiExample<string>("Formal", "Hello, Dr. Smith!")
        {
            Summary = "Formal greeting",
        };
    }
}
