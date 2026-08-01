// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Deprecation;

/// <summary>
/// Tests that <see cref="ObsoleteAttribute"/> maps onto the OpenAPI deprecated flag.
/// </summary>
[TestClass]
public class DeprecationTests
{
    /// <summary>
    /// Verifies obsolete methods are marked deprecated and others are not.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_MarksObsoleteMethodsDeprecated()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(DeprecatedMethodHub));

        var legacy = doc.Paths["/hubs/DeprecatedMethod/GetValueLegacy"].Operations[OperationType.Post];
        Assert.IsTrue(legacy.Deprecated, "An [Obsolete] method should be marked deprecated.");

        var current = doc.Paths["/hubs/DeprecatedMethod/GetValue"].Operations[OperationType.Post];
        Assert.IsFalse(current.Deprecated, "A method without [Obsolete] should not be deprecated.");
    }
}
