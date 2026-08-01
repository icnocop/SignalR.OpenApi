// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalR.OpenApi.Tests.Generation.GenerateDocument.Examples.DependencyInjection;

/// <summary>
/// Tests that examples providers with constructor dependencies are resolved from the
/// application's service provider rather than being silently skipped.
/// </summary>
[TestClass]
public class DependencyInjectionExampleTests
{
    private const string SubmitRequestPath = "/hubs/DiExample/SubmitRequest";

    /// <summary>
    /// Verifies a provider with a singleton dependency is resolved and its value used.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ExamplesProviderWithSingletonDependency_IsResolved()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITestValueProvider>(new TestValueProvider("InjectedProduct"));
        using var serviceProvider = services.BuildServiceProvider();

        var doc = GeneratorTestHelper.GenerateWithServices(serviceProvider, typeof(DiExampleHub));

        AssertExampleName(doc, "InjectedProduct");
    }

    /// <summary>
    /// Verifies a provider with a scoped dependency is resolved, which requires the
    /// generator to create a scope rather than resolving from the root provider.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_ExamplesProviderWithScopedDependency_IsResolved()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestValueProvider>(_ => new TestValueProvider("ScopedValue"));
        using var serviceProvider = services.BuildServiceProvider();

        var doc = GeneratorTestHelper.GenerateWithServices(serviceProvider, typeof(DiExampleHub));

        AssertExampleName(doc, "ScopedValue");
    }

    private static void AssertExampleName(OpenApiDocument doc, string expectedName)
    {
        var submitRequest = doc.Paths[SubmitRequestPath].Operations[OperationType.Post];

        Assert.IsNotNull(submitRequest.RequestBody);
        var jsonContent = submitRequest.RequestBody.Content["application/json"];
        Assert.IsNotNull(jsonContent.Examples);
        Assert.IsTrue(jsonContent.Examples.ContainsKey("DiExample"));

        var diExample = jsonContent.Examples["DiExample"];
        Assert.AreEqual("Example from DI provider", diExample.Summary);

        Assert.IsNotNull(diExample.Value);
        var exampleObj = diExample.Value as Microsoft.OpenApi.Any.OpenApiObject;
        Assert.IsNotNull(exampleObj);
        var nameValue = exampleObj["name"] as Microsoft.OpenApi.Any.OpenApiString;
        Assert.IsNotNull(nameValue);
        Assert.AreEqual(expectedName, nameValue.Value, "The injected value should be used.");
    }
}
