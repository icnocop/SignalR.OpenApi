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
        Assert.IsTrue(content.Contains("signalr-status"));
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

    private static async Task<IHost> CreateTestHost()
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSignalR();
                    services.AddSignalROpenApi();
                    services.AddSignalRSwaggerUi();
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
