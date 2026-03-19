// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignalR.OpenApi.Extensions;
using SignalR.OpenApi.Tests.TestHubs;

namespace SignalR.OpenApi.Tests;

/// <summary>
/// Playwright-based E2E tests verifying that apiKey security scheme
/// headers are correctly sent during SignalR connection negotiate
/// and not misinterpreted as Bearer tokens.
/// </summary>
[TestClass]
[TestCategory("Playwright")]
public class SwaggerUiApiKeyPlaywrightTests : PageTest
{
    private const string TestHeaderName = "X-Test-Header";

    private static IHost? testHost;
    private static string? baseUrl;

    /// <summary>
    /// Starts a Kestrel server with an apiKey security scheme configured.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        var port = GetAvailablePort();
        baseUrl = $"http://localhost:{port}";

        testHost = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseKestrel();
                webBuilder.UseUrls(baseUrl);
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSignalR();
                    services.AddSignalROpenApi(options =>
                    {
                        options.Assemblies = [typeof(BasicHub).Assembly];
                        options.ApiKeyHeaders[TestHeaderName] = "A test header for verifying apiKey behavior.";
                    });
                    services.AddSignalRSwaggerUi();
                    services.AddRouting();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseSignalRSwaggerUi();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<BasicHub>("/hubs/basic");
                        endpoints.MapSignalROpenApi();
                    });
                });
            })
            .Build();

        await testHost.StartAsync();
    }

    /// <summary>
    /// Stops the test server.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        if (testHost is not null)
        {
            await testHost.StopAsync();
            testHost.Dispose();
        }
    }

    /// <summary>
    /// Verifies that when an apiKey scheme is authorized, the negotiate
    /// request includes the custom header with the entered value.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_ApiKeyHeaderSentDuringNegotiate()
    {
        const string headerValue = "test-session-123";

        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // Authorize with the apiKey value
        await AuthorizeApiKeyAsync(headerValue);

        // Intercept the negotiate request to capture headers
        string? capturedHeaderValue = null;
        await Page.RouteAsync("**/negotiate**", async route =>
        {
            capturedHeaderValue = route.Request.Headers.GetValueOrDefault(TestHeaderName.ToLowerInvariant());
            await route.ContinueAsync();
        });

        // Execute a hub method to trigger connection
        await ExecuteSendMessageAsync();

        Assert.IsNotNull(capturedHeaderValue, $"Negotiate request should include {TestHeaderName} header.");
        Assert.AreEqual(headerValue, capturedHeaderValue, $"{TestHeaderName} header value should match the authorized value.");
    }

    /// <summary>
    /// Verifies that when only an apiKey scheme is authorized, the negotiate
    /// request does not include an Authorization Bearer header (the apiKey
    /// value should not be mistakenly set as a Bearer token).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_ApiKeyNotSentAsBearerToken()
    {
        const string headerValue = "test-session-456";

        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // Authorize with the apiKey value
        await AuthorizeApiKeyAsync(headerValue);

        // Intercept the negotiate request to capture the Authorization header
        string? authorizationHeader = null;
        await Page.RouteAsync("**/negotiate**", async route =>
        {
            authorizationHeader = route.Request.Headers.GetValueOrDefault("authorization");
            await route.ContinueAsync();
        });

        // Execute a hub method to trigger connection
        await ExecuteSendMessageAsync();

        // The apiKey value should NOT appear as a Bearer token
        if (authorizationHeader is not null)
        {
            Assert.IsFalse(
                authorizationHeader.Contains(headerValue),
                $"Authorization header should not contain the apiKey value. Got: {authorizationHeader}");
        }
    }

    /// <summary>
    /// Verifies that when the apiKey scheme is authorized, the hub method
    /// executes successfully without connection errors.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_ApiKeyAuthorizedThenInvokeSucceeds()
    {
        var consoleLogs = new List<string>();
        Page.Console += (_, msg) => consoleLogs.Add($"[{msg.Type}] {msg.Text}");

        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // Authorize with the apiKey value
        await AuthorizeApiKeyAsync("test-session-789");

        // Execute a hub method
        await ExecuteSendMessageAsync();

        // Wait for response
        await Page.WaitForTimeoutAsync(5000);

        var allLogs = string.Join("\n", consoleLogs);

        // Find the response
        var sendMessageOp = Page.Locator(".opblock", new() { HasTextString = "SendMessage" });
        var responseBody = sendMessageOp.Locator(".responses-wrapper .response-col_description pre");
        var count = await responseBody.CountAsync();

        Assert.IsTrue(count > 0, $"Response should render. Console logs:\n{allLogs}");

        var bodyText = await responseBody.First.TextContentAsync();

        Assert.IsFalse(
            bodyText?.Contains("Connection failed") == true,
            $"Should not get connection error. Body: {bodyText}\nConsole logs:\n{allLogs}");
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private async Task AuthorizeApiKeyAsync(string value)
    {
        // Click the Authorize button in SwaggerUI
        var authorizeButton = Page.Locator(".btn.authorize");
        await authorizeButton.WaitForAsync(new() { Timeout = 10000 });
        await authorizeButton.ClickAsync();

        // Wait for the auth dialog
        var authDialog = Page.Locator(".dialog-ux .modal-ux");
        await authDialog.WaitForAsync(new() { Timeout = 5000 });

        // Find the apiKey input field and enter the value
        var apiKeyInput = authDialog.Locator("input[type='text']");
        await apiKeyInput.First.FillAsync(value);

        // Click the Authorize button inside the dialog
        var authorizeConfirm = authDialog.Locator("button.btn.modal-btn.auth.authorize");
        await authorizeConfirm.First.ClickAsync();

        // Wait briefly for auth to be applied
        await Page.WaitForTimeoutAsync(500);

        // Close the dialog
        var closeButton = authDialog.Locator("button.btn.modal-btn.auth.btn-done");
        await closeButton.ClickAsync();
    }

    private async Task ExecuteSendMessageAsync()
    {
        // Find and expand the SendMessage operation
        var sendMessageOp = Page.Locator(".opblock", new() { HasTextString = "SendMessage" });
        await sendMessageOp.First.ClickAsync();

        // Click "Try it out"
        var tryItOutButton = sendMessageOp.Locator("button", new() { HasTextString = "Try it out" });
        await tryItOutButton.ClickAsync();

        // Fill in the message parameter
        var textarea = sendMessageOp.Locator("textarea.body-param__text");
        await textarea.ClearAsync();
        await textarea.FillAsync("{\"message\": \"hello\"}");

        // Click "Execute"
        var executeButton = sendMessageOp.Locator("button.execute");
        await executeButton.ClickAsync();
    }
}
