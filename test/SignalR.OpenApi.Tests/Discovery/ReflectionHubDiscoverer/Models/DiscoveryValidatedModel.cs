// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace SignalR.OpenApi.Tests.Discovery;

/// <summary>
/// A model with data annotations, used to verify parameter type discovery.
/// </summary>
public class DiscoveryValidatedModel
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the age.
    /// </summary>
    [Range(0, 150)]
    public int? Age { get; set; }
}
