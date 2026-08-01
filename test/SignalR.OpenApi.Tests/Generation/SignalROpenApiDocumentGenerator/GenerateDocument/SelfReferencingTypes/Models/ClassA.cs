// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.SelfReferencingTypes;

/// <summary>
/// A concrete polymorphic base type that lists itself as one of its own derived types.
/// This is valid System.Text.Json — it is required when instances of the base type are
/// serialized as-is, because otherwise the serializer rejects the base type.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ClassA), nameof(ClassA))]
[JsonDerivedType(typeof(ClassB), nameof(ClassB))]
public class ClassA
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public required string Name { get; set; }
}
