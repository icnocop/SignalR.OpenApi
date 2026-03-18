// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A hub for testing enum schema generation.
/// </summary>
public class EnumHub : Hub
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    public Task<TestStatus> GetStatus()
    {
        return Task.FromResult(TestStatus.Active);
    }

    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>A test result with an enum property.</returns>
    public Task<TestResult> GetResult()
    {
        return Task.FromResult(new TestResult { Message = "OK", Status = TestStatus.Completed });
    }

    /// <summary>
    /// Updates the status.
    /// </summary>
    /// <param name="status">The new status.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UpdateStatus(TestStatus status)
    {
        return Task.CompletedTask;
    }
}
