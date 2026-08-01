// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.SignalR;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.MultipleObjectParameters;

/// <summary>
/// A hub whose methods mix complex object parameters with each other and with primitives.
/// </summary>
public class MultipleObjectParameterHub : Hub
{
    /// <summary>
    /// Compares two addresses.
    /// </summary>
    /// <param name="first">The first address.</param>
    /// <param name="second">The second address.</param>
    /// <returns>A combined description.</returns>
    public Task<string> CompareAddresses(ShippingAddress first, ShippingAddress second)
    {
        return Task.FromResult($"{first.City} and {second.City}");
    }

    /// <summary>
    /// Ships to an address with an additional note.
    /// </summary>
    /// <param name="address">The destination address.</param>
    /// <param name="note">An additional note.</param>
    /// <returns>A confirmation message.</returns>
    public Task<string> ShipWithNote(ShippingAddress address, string note)
    {
        return Task.FromResult($"{address.City} — {note}");
    }
}
