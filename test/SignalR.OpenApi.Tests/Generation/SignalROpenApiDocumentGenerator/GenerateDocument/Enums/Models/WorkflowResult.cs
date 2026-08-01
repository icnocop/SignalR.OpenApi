// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Enums;

/// <summary>
/// A model with an enum property.
/// </summary>
public class WorkflowResult
{
    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public WorkflowStatus Status { get; set; }
}
