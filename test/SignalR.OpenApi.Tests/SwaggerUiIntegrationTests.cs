// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignalR.OpenApi.Extensions;
using SignalR.OpenApi.SwaggerUi;

namespace SignalR.OpenApi.Tests;

/// <summary>
/// Tests for the SignalR SwaggerUI integration.
/// </summary>
[TestClass]
public class SwaggerUiIntegrationTests
{
    /// <summary>
    /// Verifies that AddSignalRSwaggerUi registers options in DI.
    /// </summary>
    [TestMethod]
    public void AddSignalRSwaggerUi_RegistersOptions()
    {
        var services = new ServiceCollection();
        services.AddSignalRSwaggerUi(o =>
        {
            o.RoutePrefix = "my-swagger";
            o.DocumentTitle = "My Hubs";
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SignalRSwaggerUiOptions>>().Value;

        Assert.AreEqual("my-swagger", options.RoutePrefix);
        Assert.AreEqual("My Hubs", options.DocumentTitle);
    }

    /// <summary>
    /// Verifies default option values.
    /// </summary>
    [TestMethod]
    public void AddSignalRSwaggerUi_DefaultOptions()
    {
        var services = new ServiceCollection();
        services.AddSignalRSwaggerUi();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SignalRSwaggerUiOptions>>().Value;

        Assert.AreEqual("signalr-swagger", options.RoutePrefix);
        Assert.AreEqual("/openapi/signalr-v1.json", options.SpecUrl);
        Assert.AreEqual("SignalR API", options.DocumentTitle);
        Assert.IsFalse(options.UseDefaultCredentials);
        Assert.IsTrue(options.StripAsyncSuffix);
        Assert.AreEqual(0, options.Headers.Count);
        Assert.IsTrue(options.SyntaxHighlight);
        Assert.AreEqual(-1, options.DefaultModelsExpandDepth);
        Assert.AreEqual(DocExpansion.List, options.DocExpansion);
        Assert.IsFalse(options.SortTagsAlphabetically);
        Assert.IsFalse(options.SortOperationsAlphabetically);
    }

    /// <summary>
    /// Verifies that custom headers can be configured.
    /// </summary>
    [TestMethod]
    public void AddSignalRSwaggerUi_CustomHeaders()
    {
        var services = new ServiceCollection();
        services.AddSignalRSwaggerUi(o =>
        {
            o.Headers["X-Custom-Header"] = "TestValue";
            o.Headers["X-Another"] = "Other";
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SignalRSwaggerUiOptions>>().Value;

        Assert.AreEqual(2, options.Headers.Count);
        Assert.AreEqual("TestValue", options.Headers["X-Custom-Header"]);
        Assert.AreEqual("Other", options.Headers["X-Another"]);
    }

    /// <summary>
    /// Verifies that syntax highlighting can be disabled.
    /// </summary>
    [TestMethod]
    public void AddSignalRSwaggerUi_SyntaxHighlightDisabled()
    {
        var services = new ServiceCollection();
        services.AddSignalRSwaggerUi(o =>
        {
            o.SyntaxHighlight = false;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SignalRSwaggerUiOptions>>().Value;

        Assert.IsFalse(options.SyntaxHighlight);
    }

    /// <summary>
    /// Verifies that the default models expand depth can be customized.
    /// </summary>
    [TestMethod]
    public void AddSignalRSwaggerUi_CustomDefaultModelsExpandDepth()
    {
        var services = new ServiceCollection();
        services.AddSignalRSwaggerUi(o =>
        {
            o.DefaultModelsExpandDepth = 2;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SignalRSwaggerUiOptions>>().Value;

        Assert.AreEqual(2, options.DefaultModelsExpandDepth);
    }

    /// <summary>
    /// Verifies that DocExpansion can be configured via DI.
    /// </summary>
    [TestMethod]
    public void AddSignalRSwaggerUi_CustomDocExpansion()
    {
        var services = new ServiceCollection();
        services.AddSignalRSwaggerUi(o =>
        {
            o.DocExpansion = DocExpansion.None;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SignalRSwaggerUiOptions>>().Value;

        Assert.AreEqual(DocExpansion.None, options.DocExpansion);
    }

    /// <summary>
    /// Verifies that SortTagsAlphabetically can be configured via DI.
    /// </summary>
    [TestMethod]
    public void AddSignalRSwaggerUi_SortTagsAlphabetically()
    {
        var services = new ServiceCollection();
        services.AddSignalRSwaggerUi(o =>
        {
            o.SortTagsAlphabetically = true;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SignalRSwaggerUiOptions>>().Value;

        Assert.IsTrue(options.SortTagsAlphabetically);
    }

    /// <summary>
    /// Verifies that SortOperationsAlphabetically can be configured via DI.
    /// </summary>
    [TestMethod]
    public void AddSignalRSwaggerUi_SortOperationsAlphabetically()
    {
        var services = new ServiceCollection();
        services.AddSignalRSwaggerUi(o =>
        {
            o.SortOperationsAlphabetically = true;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SignalRSwaggerUiOptions>>().Value;

        Assert.IsTrue(options.SortOperationsAlphabetically);
    }

    /// <summary>
    /// Verifies embedded signalr.min.js resource is served.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task EmbeddedResources_ServesSignalRJs()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/_resources/signalr.min.js");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("signalR") || content.Contains("HubConnection"));
    }

    /// <summary>
    /// Verifies embedded plugin JS resource is served.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task EmbeddedResources_ServesPluginJs()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/_resources/signalr-openapi-plugin.js");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("SignalROpenApiPlugin"));
    }

    /// <summary>
    /// Verifies embedded CSS resource is served.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task EmbeddedResources_ServesCss()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/_resources/signalr-openapi.css");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("signalr-connect-btn"));
    }

    /// <summary>
    /// Verifies the SwaggerUI page is served at the configured route prefix.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_ServesAtRoutePrefix()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.html");

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(content.Contains("swagger-ui"));
    }

    /// <summary>
    /// Verifies the SwaggerUI page includes injected JS and CSS references.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_InjectsJsAndCss()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.html");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("signalr.min.js"), "Should inject signalr.min.js");
        Assert.IsTrue(content.Contains("signalr-openapi-plugin.js"), "Should inject plugin JS");
        Assert.IsTrue(content.Contains("signalr-openapi.css"), "Should inject CSS");
    }

    /// <summary>
    /// Verifies the SwaggerUI configObject registers SignalROpenApiPlugin.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_RegistersPluginInConfigObject()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        // The ConfigObject JSON is rendered in Swashbuckle's index.js, not index.html.
        using var response = await client.GetAsync("/signalr-swagger/index.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("SignalROpenApiPlugin"), "ConfigObject should reference SignalROpenApiPlugin in index.js");
    }

    /// <summary>
    /// Verifies that syntax highlighting is not disabled when SyntaxHighlight is true (default).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_SyntaxHighlightEnabledByDefault()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsFalse(content.Contains("\"syntaxHighlight\":false"), "Syntax highlighting should be enabled by default");
    }

    /// <summary>
    /// Verifies that syntax highlighting is disabled when configured.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_SyntaxHighlightDisabledWhenConfigured()
    {
        using var host = await CreateTestHost(o => o.SyntaxHighlight = false);
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("syntaxHighlight"), "Syntax highlighting should be disabled in config");
    }

    /// <summary>
    /// Verifies that the default models expand depth is set to -1 by default.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_DefaultModelsExpandDepthHiddenByDefault()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("defaultModelsExpandDepth"), "Config should include defaultModelsExpandDepth");
    }

    /// <summary>
    /// Verifies that a custom default models expand depth is applied.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_CustomDefaultModelsExpandDepthApplied()
    {
        using var host = await CreateTestHost(o => o.DefaultModelsExpandDepth = 2);
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("defaultModelsExpandDepth"), "Config should include defaultModelsExpandDepth");
    }

    /// <summary>
    /// Verifies that DocExpansion defaults to "list" in the SwaggerUI configObject.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_DocExpansionDefaultIsList()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("\"docExpansion\":\"list\""), "Default DocExpansion should be list");
    }

    /// <summary>
    /// Verifies that DocExpansion.None is applied to the SwaggerUI configObject.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_DocExpansionNoneApplied()
    {
        using var host = await CreateTestHost(o => o.DocExpansion = DocExpansion.None);
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("\"docExpansion\":\"none\""), "DocExpansion should be none");
    }

    /// <summary>
    /// Verifies that tagsSorter is not present by default.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_TagsSorterNotPresentByDefault()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsFalse(content.Contains("tagsSorter"), "tagsSorter should not be present by default");
    }

    /// <summary>
    /// Verifies that tagsSorter is set to "alpha" when SortTagsAlphabetically is true.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_SortTagsAlphabeticallyApplied()
    {
        using var host = await CreateTestHost(o => o.SortTagsAlphabetically = true);
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("\"tagsSorter\":\"alpha\""), "tagsSorter should be alpha when enabled");
    }

    /// <summary>
    /// Verifies that operationsSorter is not present by default.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_OperationsSorterNotPresentByDefault()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsFalse(content.Contains("operationsSorter"), "operationsSorter should not be present by default");
    }

    /// <summary>
    /// Verifies that operationsSorter is set to "alpha" when SortOperationsAlphabetically is true.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_SortOperationsAlphabeticallyApplied()
    {
        using var host = await CreateTestHost(o => o.SortOperationsAlphabetically = true);
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/index.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("\"operationsSorter\":\"alpha\""), "operationsSorter should be alpha when enabled");
    }

    /// <summary>
    /// Verifies that the document includes hubPath in x-signalr extension.
    /// </summary>
    [TestMethod]
    public void GenerateDocument_IncludesHubPath()
    {
        var options = new SignalROpenApiOptions
        {
            Assemblies = [typeof(TestHubs.BasicHub).Assembly],
        };

        options.HubRoutes[typeof(TestHubs.BasicHub)] = "/hubs/basic";

        var opts = Options.Create(options);
        var discoverer = new Discovery.ReflectionHubDiscoverer(opts);
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var generator = new Generation.SignalROpenApiDocumentGenerator(opts, serviceProvider);

        var hubs = discoverer.DiscoverHubs();
        var doc = generator.GenerateDocument(hubs);

        var sendMessage = doc.Paths["/hubs/Basic/SendMessage"]
            .Operations[Microsoft.OpenApi.Models.OperationType.Post];
        var extension = (Microsoft.OpenApi.Any.OpenApiObject)sendMessage.Extensions["x-signalr"];

        Assert.IsTrue(extension.ContainsKey("hubPath"));
        Assert.AreEqual("/hubs/basic", ((Microsoft.OpenApi.Any.OpenApiString)extension["hubPath"]).Value);
    }

    /// <summary>
    /// Verifies that the plugin JS contains the _disconnectHub function.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task PluginJs_ContainsDisconnectHubFunction()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/_resources/signalr-openapi-plugin.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("_disconnectHub"), "Plugin JS should contain _disconnectHub function");
    }

    /// <summary>
    /// Verifies that the plugin JS contains the auth fingerprint mechanism.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task PluginJs_ContainsAuthFingerprint()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/_resources/signalr-openapi-plugin.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("_computeAuthFingerprint"), "Plugin JS should contain _computeAuthFingerprint function");
        Assert.IsTrue(content.Contains("_hubAuthFingerprints"), "Plugin JS should track auth fingerprints per hub");
    }

    /// <summary>
    /// Verifies that the plugin JS contains the SignalRHubConnectionBar component.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task PluginJs_ContainsConnectionBarComponent()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/_resources/signalr-openapi-plugin.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("SignalRHubConnectionBar"), "Plugin JS should contain SignalRHubConnectionBar component");
    }

    /// <summary>
    /// Verifies that the plugin JS contains the OperationTag wrapper.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task PluginJs_ContainsOperationTagWrapper()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/_resources/signalr-openapi-plugin.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("OperationTag"), "Plugin JS should contain OperationTag wrapper component");
    }

    /// <summary>
    /// Verifies that the plugin JS contains the _getTagHubMap function
    /// which builds a tag-to-hub mapping for custom [Tags] support.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task PluginJs_ContainsTagHubMapFunction()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/_resources/signalr-openapi-plugin.js");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("_getTagHubMap"), "Plugin JS should contain _getTagHubMap function for custom [Tags] support");
    }

    /// <summary>
    /// Verifies that the CSS contains connection bar styles.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task Css_ContainsConnectionBarStyles()
    {
        using var host = await CreateTestHost();
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/signalr-swagger/_resources/signalr-openapi.css");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("signalr-hub-connection-bar"), "CSS should contain connection bar styles");
        Assert.IsTrue(content.Contains("signalr-connect-btn"), "CSS should contain connect button styles");
        Assert.IsTrue(content.Contains("signalr-disconnect-btn"), "CSS should contain disconnect button styles");
    }

    private static async Task<IHost> CreateTestHost(Action<SignalRSwaggerUiOptions>? configureUi = null)
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSignalR();
                    services.AddSignalROpenApi();
                    services.AddSignalRSwaggerUi(configureUi ?? (_ => { }));
                    services.AddRouting();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseSignalRSwaggerUi();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapSignalROpenApi();
                    });
                });
            })
            .StartAsync();
    }
}
