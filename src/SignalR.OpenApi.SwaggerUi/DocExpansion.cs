// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.SwaggerUi;

/// <summary>
/// Controls the default expansion setting for the operations and tags in SwaggerUI.
/// </summary>
public enum DocExpansion
{
    /// <summary>
    /// Expands only the tags (groups). Operations within each tag are collapsed.
    /// This is the default SwaggerUI behavior.
    /// </summary>
    List,

    /// <summary>
    /// Expands the tags and operations fully.
    /// </summary>
    Full,

    /// <summary>
    /// Collapses everything (both tags and operations).
    /// </summary>
    None,
}
