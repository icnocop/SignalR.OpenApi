// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.TestHubs;

/// <summary>
/// A tree node with self-referencing children.
/// </summary>
public class TreeNode
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent node (self-reference).
    /// </summary>
    public TreeNode? Parent { get; set; }

    /// <summary>
    /// Gets or sets the child nodes (self-referencing collection).
    /// </summary>
    public List<TreeNode>? Children { get; set; }
}
