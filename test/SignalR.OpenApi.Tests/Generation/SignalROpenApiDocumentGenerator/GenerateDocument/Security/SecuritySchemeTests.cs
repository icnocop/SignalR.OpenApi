// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Security;

/// <summary>
/// Tests for emitting security schemes and per-operation security requirements.
/// </summary>
[TestClass]
public class SecuritySchemeTests
{
    /// <summary>
    /// Verifies a configured HTTP bearer scheme is emitted when an authorized hub exists.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenAuthorizedHubAndBearerConfigured_AddsScheme()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o => o.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Bearer token.",
            },
            typeof(SecuredHub));

        Assert.IsNotNull(doc.Components.SecuritySchemes);
        Assert.IsTrue(doc.Components.SecuritySchemes.ContainsKey("Bearer"));
        Assert.AreEqual(SecuritySchemeType.Http, doc.Components.SecuritySchemes["Bearer"].Type);
    }

    /// <summary>
    /// Verifies a configured apiKey scheme is emitted with its location and header name.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenApiKeySchemeConfigured_AddsScheme()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o => o.SecuritySchemes["Impersonation"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-Session-Id",
                Description = "Session ID for impersonation.",
            },
            typeof(SecuredHub));

        Assert.IsNotNull(doc.Components.SecuritySchemes);
        var scheme = doc.Components.SecuritySchemes["Impersonation"];
        Assert.AreEqual(SecuritySchemeType.ApiKey, scheme.Type);
        Assert.AreEqual(ParameterLocation.Header, scheme.In);
        Assert.AreEqual("X-Session-Id", scheme.Name);
        Assert.AreEqual("Session ID for impersonation.", scheme.Description);
    }

    /// <summary>
    /// Verifies apiKey and bearer schemes can coexist.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenApiKeyAndBearerConfigured_AddsBoth()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o =>
            {
                o.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                };
                o.SecuritySchemes["Impersonation"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-Session-Id",
                };
            },
            typeof(SecuredHub));

        Assert.IsNotNull(doc.Components.SecuritySchemes);
        Assert.AreEqual(2, doc.Components.SecuritySchemes.Count);
        Assert.IsTrue(doc.Components.SecuritySchemes.ContainsKey("Bearer"));
        Assert.IsTrue(doc.Components.SecuritySchemes.ContainsKey("Impersonation"));
    }

    /// <summary>
    /// Verifies no HTTP auth schemes appear when none are configured, even though an
    /// authorized hub exists.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_WhenNoSchemesConfigured_AddsNoHttpSchemes()
    {
        var doc = GeneratorTestHelper.GenerateFor(typeof(SecuredHub));

        if (doc.Components.SecuritySchemes is not null)
        {
            foreach (var scheme in doc.Components.SecuritySchemes.Values)
            {
                Assert.AreNotEqual(
                    SecuritySchemeType.Http,
                    scheme.Type,
                    "Should not have HTTP auth schemes when SecuritySchemes is empty.");
            }
        }
    }

    /// <summary>
    /// Verifies authorized methods carry a security requirement.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SetsSecurityOnAuthorizedMethods()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o => o.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
            },
            typeof(SecuredHub));

        var getUser = doc.Paths["/hubs/Secured/GetUserDetails"].Operations[OperationType.Post];

        Assert.IsNotNull(getUser.Security);
        Assert.IsTrue(getUser.Security.Count > 0);
    }

    /// <summary>
    /// Verifies the security requirement references every configured scheme.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_SecurityRequirementsIncludeAllConfiguredSchemes()
    {
        var doc = GeneratorTestHelper.GenerateFor(
            o =>
            {
                o.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                };
                o.SecuritySchemes["Impersonation"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-Session-Id",
                };
            },
            typeof(SecuredHub));

        var getUser = doc.Paths["/hubs/Secured/GetUserDetails"].Operations[OperationType.Post];

        Assert.IsNotNull(getUser.Security);
        Assert.AreEqual(1, getUser.Security.Count, "Should have one security requirement.");

        var schemeIds = getUser.Security[0].Keys.Select(k => k.Reference.Id).ToList();
        CollectionAssert.Contains(schemeIds, "Bearer");
        CollectionAssert.Contains(schemeIds, "Impersonation");
    }
}
