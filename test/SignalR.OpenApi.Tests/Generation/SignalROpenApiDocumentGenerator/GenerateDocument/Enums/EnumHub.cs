// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Enums;

/// <summary>
/// A hub returning enums directly and as a property of a complex type.
/// </summary>
public class EnumHub : Hub
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public Task<WorkflowStatus> GetStatus()
    {
        return Task.FromResult(WorkflowStatus.Active);
    }

    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>A result with an enum property.</returns>
    public Task<WorkflowResult> GetResult()
    {
        return Task.FromResult(new WorkflowResult { Message = "OK", Status = WorkflowStatus.Completed });
    }
}
