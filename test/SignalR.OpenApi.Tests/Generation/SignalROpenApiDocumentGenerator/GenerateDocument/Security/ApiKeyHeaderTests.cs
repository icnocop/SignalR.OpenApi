// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Security;

/// <summary>
/// Tests for the <c>ApiKeyHeaders</c> option, which emits apiKey security schemes
/// independently of hub authorization.
/// </summary>
[TestClass]
public class ApiKeyHeaderTests
{
    /// <summary>
    /// Verifies configured API key headers become apiKey security schemes.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_AddsApiKeyHeaderSchemes()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o =>
            {
                o.ApiKeyHeaders["X-Custom-Header"] = "A custom header.";
                o.ApiKeyHeaders["X-Tenant-Id"] = "Tenant identifier.";
            },
            typeof(SecuredHub));

        Assert.IsNotNull(doc.Components.SecuritySchemes);
        Assert.IsTrue(doc.Components.SecuritySchemes.ContainsKey("X-Custom-Header"));
        Assert.IsTrue(doc.Components.SecuritySchemes.ContainsKey("X-Tenant-Id"));

        var customHeader = doc.Components.SecuritySchemes["X-Custom-Header"];
        Assert.AreEqual(SecuritySchemeType.ApiKey, customHeader.Type);
        Assert.AreEqual(ParameterLocation.Header, customHeader.In);
        Assert.AreEqual("X-Custom-Header", customHeader.Name);
        Assert.AreEqual("A custom header.", customHeader.Description);
    }

    /// <summary>
    /// Verifies no apiKey schemes are added when the option is empty.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_NoApiKeyHeaders_DoesNotAddApiKeySchemes()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(SecuredHub));

        if (doc.Components.SecuritySchemes is not null)
        {
            foreach (var scheme in doc.Components.SecuritySchemes.Values)
            {
                Assert.AreNotEqual(
                    SecuritySchemeType.ApiKey,
                    scheme.Type,
                    "Should not have apiKey schemes when ApiKeyHeaders is empty.");
            }
        }
    }
}
