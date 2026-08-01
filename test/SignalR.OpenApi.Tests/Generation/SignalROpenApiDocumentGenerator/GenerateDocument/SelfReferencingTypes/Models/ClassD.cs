// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.SelfReferencingTypes;

/// <summary>
/// A derived type of <see cref="ClassC"/>.
/// </summary>
public class ClassD : ClassC
{
    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string? Text { get; set; }
}
