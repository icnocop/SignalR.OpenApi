// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;
using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Examples.DependencyInjection;

/// <summary>
/// A hub whose examples provider is resolved from the service provider.
/// </summary>
public class DiExampleHub : Hub
{
    /// <summary>
    /// Submits a request.
    /// </summary>
    /// <param name="request">The request to submit.</param>
    /// <returns>The request identifier.</returns>
    [SignalROpenApiRequestExamples(typeof(DiRequestExamplesProvider))]
    public Task<string> SubmitRequest(DiRequest request)
    {
        _ = request;
        return Task.FromResult("REQ-001");
    }
}
