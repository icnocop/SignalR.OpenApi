// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.SelfReferencingTypes;

/// <summary>
/// A derived type of <see cref="ClassA"/>.
/// </summary>
public class ClassB : ClassA
{
    /// <summary>
    /// Gets or sets the order.
    /// </summary>
    public int Order { get; set; }
}
