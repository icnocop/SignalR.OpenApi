// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.SelfReferencingTypes;

/// <summary>
/// An abstract polymorphic base type that is reachable from itself through a property,
/// so the schema walk cycles back into the same polymorphic hierarchy.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ClassD), nameof(ClassD))]
public abstract class ClassC
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the next item in the chain, which cycles back into this hierarchy.
    /// </summary>
    public ClassC? Next { get; set; }
}
