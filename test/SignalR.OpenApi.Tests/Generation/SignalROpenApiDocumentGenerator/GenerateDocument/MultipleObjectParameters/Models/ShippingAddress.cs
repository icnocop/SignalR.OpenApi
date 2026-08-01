// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.MultipleObjectParameters;

/// <summary>
/// A simple flat model used as a complex parameter.
/// </summary>
public class ShippingAddress
{
    /// <summary>
    /// Gets or sets the street.
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;
}
