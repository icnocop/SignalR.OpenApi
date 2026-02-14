// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.FluentValidation.Tests.TestModels;

/// <summary>
/// Test model for nested validator support.
/// </summary>
public class Address
{
    /// <summary>
    /// Gets or sets the street.
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zip code.
    /// </summary>
    public string ZipCode { get; set; } = string.Empty;
}
