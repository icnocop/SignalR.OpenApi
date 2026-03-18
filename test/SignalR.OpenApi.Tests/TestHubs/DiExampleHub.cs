// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;
using SignalR.OpenApi.Examples;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A hub demonstrating DI-based example provider attributes.
/// </summary>
public class DiExampleHub : Hub
{
    /// <summary>
    /// Submits a request using a DI-based examples provider.
    /// </summary>
    /// <param name="request">The request to submit.</param>
    /// <returns>The request identifier.</returns>
    [SignalROpenApiRequestExamples(typeof(DiOrderRequestExamplesProvider))]
    public Task<string> SubmitRequest(DiRequest request)
    {
        return Task.FromResult("REQ-001");
    }
}
