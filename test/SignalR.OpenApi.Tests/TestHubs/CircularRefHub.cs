// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A hub for testing circular reference handling.
/// </summary>
public class CircularRefHub : Hub
{
    /// <summary>
    /// Processes a tree node with self-referencing children.
    /// </summary>
    /// <param name="node">The tree node.</param>
    /// <returns>The processed node.</returns>
    public Task<TreeNode> ProcessNode(TreeNode node)
    {
        return Task.FromResult(node);
    }
}
