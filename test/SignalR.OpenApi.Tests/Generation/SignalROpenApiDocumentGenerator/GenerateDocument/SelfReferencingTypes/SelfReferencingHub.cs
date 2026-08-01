// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.SelfReferencingTypes;

/// <summary>
/// A hub whose parameters reference themselves, directly and through polymorphic
/// hierarchies, for testing that schema generation terminates.
/// </summary>
public class SelfReferencingHub : Hub
{
    /// <summary>
    /// Accepts a plain self-referencing type.
    /// </summary>
    /// <param name="node">The tree node.</param>
    /// <returns>The processed node.</returns>
    public Task<TreeNode> ProcessNode(TreeNode node)
    {
        return Task.FromResult(node);
    }

    /// <summary>
    /// Accepts a type that lists itself as one of its own derived types.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A description of the value.</returns>
    public Task<string> SubmitA(ClassA value)
    {
        return Task.FromResult(value.GetType().Name);
    }

    /// <summary>
    /// Accepts a polymorphic type that is reachable from itself through a property.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A completed task.</returns>
    public Task SubmitC(ClassC value)
    {
        _ = value;
        return Task.CompletedTask;
    }
}
