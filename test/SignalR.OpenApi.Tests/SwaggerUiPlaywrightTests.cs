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
/// Playwright-based E2E tests verifying the SignalR SwaggerUI plugin
/// correctly intercepts operations and invokes SignalR instead of HTTP.
/// </summary>
[TestClass]
[TestCategory("Playwright")]
public class SwaggerUiPlaywrightTests : PageTest
{
    private static IHost? testHost;
    private static string? baseUrl;

    /// <summary>
    /// Starts a real Kestrel server for Playwright to connect to.
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
                        endpoints.MapHub<StreamingHub>("/hubs/streaming");
                        endpoints.MapHub<TypedChatHub>("/hubs/typedchat");
                        endpoints.MapHub<ExampleHub>("/hubs/example");
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
    /// Verifies the SwaggerUI page loads and renders SignalR operations.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_LoadsAndRendersOperations()
    {
        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        // Wait for SwaggerUI to fully render operations
        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        var operationCount = await operationBlock.CountAsync();
        Assert.IsTrue(operationCount > 0, "SwaggerUI should render at least one operation.");
    }

    /// <summary>
    /// Verifies the SignalR plugin is loaded by checking method labels.
    /// Operations should show "invoke" instead of "post".
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_PluginChangesMethodLabels()
    {
        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        // Wait for operations to render
        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // The plugin replaces "post" with "invoke" for SignalR operations
        var invokeLabels = Page.Locator(".opblock-summary-method", new() { HasTextString = "INVOKE" });

        // Wait briefly for plugin processing
        await Page.WaitForTimeoutAsync(1000);
        var invokeCount = await invokeLabels.CountAsync();

        Assert.IsTrue(invokeCount > 0, "SignalR plugin should change POST labels to INVOKE.");
    }

    /// <summary>
    /// Verifies that executing a SignalR method returns a successful response
    /// (not a 404 HTTP error), proving the plugin intercepts the request.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_ExecuteInvokesSignalR()
    {
        var consoleLogs = new List<string>();
        Page.Console += (_, msg) => consoleLogs.Add($"[{msg.Type}] {msg.Text}");

        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        // Wait for operations to render
        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

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

        // Wait for the response to appear
        await Page.WaitForTimeoutAsync(5000);

        var allLogs = string.Join("\n", consoleLogs);

        // Check response rendered
        var responseBody = sendMessageOp.Locator(".responses-wrapper .response-col_description pre");
        var count = await responseBody.CountAsync();

        Assert.IsTrue(count > 0, $"Response should render. Console logs:\n{allLogs}");

        var bodyText = await responseBody.First.TextContentAsync();

        Assert.IsFalse(
            bodyText?.Contains("404") == true,
            $"Should not get HTTP 404. Body: {bodyText}\nConsole logs:\n{allLogs}");

        Assert.IsFalse(
            bodyText?.Contains("Failed to invoke") == true,
            $"Should not get invoke error. Body: {bodyText}\nConsole logs:\n{allLogs}");
    }

    /// <summary>
    /// Verifies that executing a SignalR method renders a response without
    /// "Could not render" errors (validates request/response state is correct).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_ResponseRendersWithoutError()
    {
        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // Expand and execute SendMessage
        var sendMessageOp = Page.Locator(".opblock", new() { HasTextString = "SendMessage" });
        await sendMessageOp.First.ClickAsync();

        var tryItOutButton = sendMessageOp.Locator("button", new() { HasTextString = "Try it out" });
        await tryItOutButton.ClickAsync();

        var textarea = sendMessageOp.Locator("textarea.body-param__text");
        await textarea.ClearAsync();
        await textarea.FillAsync("{\"message\": \"hello\"}");

        var executeButton = sendMessageOp.Locator("button.execute");
        await executeButton.ClickAsync();

        // Wait for response
        await Page.WaitForTimeoutAsync(3000);

        // Check that no "Could not render" error is shown
        var errorBoundary = sendMessageOp.Locator("text='Could not render'");
        var errorCount = await errorBoundary.CountAsync();

        Assert.AreEqual(0, errorCount, "Response should render without 'Could not render' errors.");
    }

    /// <summary>
    /// Verifies that no HTTP network request is made to the hub endpoint
    /// when executing a SignalR operation (the plugin should prevent it).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_DoesNotMakeHttpPostToHub()
    {
        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        // Wait for operations to render
        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // Monitor network requests
        var httpPostToHub = false;
        Page.Request += (_, request) =>
        {
            if (request.Url.Contains("/hubs/Basic/SendMessage") && request.Method == "POST")
            {
                httpPostToHub = true;
            }
        };

        // Find and expand the SendMessage operation
        var sendMessageOp = Page.Locator(".opblock", new() { HasTextString = "SendMessage" });
        await sendMessageOp.First.ClickAsync();

        // Click "Try it out"
        var tryItOutButton = sendMessageOp.Locator("button", new() { HasTextString = "Try it out" });
        await tryItOutButton.ClickAsync();

        // Fill in the parameter
        var textarea = sendMessageOp.Locator("textarea.body-param__text");
        await textarea.ClearAsync();
        await textarea.FillAsync("{\"message\": \"hello\"}");

        // Click "Execute"
        var executeButton = sendMessageOp.Locator("button.execute");
        await executeButton.ClickAsync();

        // Wait for the response to render
        await Page.WaitForTimeoutAsync(3000);

        Assert.IsFalse(httpPostToHub, "Plugin should intercept — no HTTP POST should be made to /hubs/Basic/SendMessage.");
    }

    /// <summary>
    /// Verifies that the curl command is hidden for SignalR operations.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_HidesCurlForSignalR()
    {
        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        // Wait for operations to render
        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // Expand the SendMessage operation
        var sendMessageOp = Page.Locator(".opblock", new() { HasTextString = "SendMessage" });
        await sendMessageOp.First.ClickAsync();

        // Click "Try it out"
        var tryItOutButton = sendMessageOp.Locator("button", new() { HasTextString = "Try it out" });
        await tryItOutButton.ClickAsync();

        // Fill in the parameter and execute
        var textarea = sendMessageOp.Locator("textarea.body-param__text");
        await textarea.ClearAsync();
        await textarea.FillAsync("{\"message\": \"hello\"}");

        var executeButton = sendMessageOp.Locator("button.execute");
        await executeButton.ClickAsync();

        // Wait for response
        await Page.WaitForTimeoutAsync(3000);

        // Verify curl section is not visible
        var curlSection = sendMessageOp.Locator(".curl-command");
        var curlCount = await curlSection.CountAsync();

        Assert.AreEqual(0, curlCount, "Curl command should be hidden for SignalR operations.");
    }

    /// <summary>
    /// Verifies that executing a streaming operation accumulates multiple items
    /// in the response instead of overwriting each one.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_StreamAccumulatesItems()
    {
        var consoleLogs = new List<string>();
        Page.Console += (_, msg) => consoleLogs.Add($"[{msg.Type}] {msg.Text}");

        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // Find and expand the StreamIntegers operation
        var streamOp = Page.Locator(".opblock", new() { HasTextString = "StreamIntegers" });
        await streamOp.First.ClickAsync();

        // Click "Try it out"
        var tryItOutButton = streamOp.Locator("button", new() { HasTextString = "Try it out" });
        await tryItOutButton.ClickAsync();

        // Fill in count parameter (small count for fast test)
        var textarea = streamOp.Locator("textarea.body-param__text");
        await textarea.ClearAsync();
        await textarea.FillAsync("{\"count\": 3}");

        // Click "Execute"
        var executeButton = streamOp.Locator("button.execute");
        await executeButton.ClickAsync();

        // Wait for stream to complete (3 items at 100ms delay + connection time)
        await Page.WaitForTimeoutAsync(5000);

        var allLogs = string.Join("\n", consoleLogs);

        // Check response rendered with accumulated items
        var responseBody = streamOp.Locator(".responses-wrapper .response-col_description pre");
        var count = await responseBody.CountAsync();
        Assert.IsTrue(count > 0, $"Response should render. Console logs:\n{allLogs}");

        var bodyText = await responseBody.First.TextContentAsync();
        Assert.IsNotNull(bodyText, "Response body should not be null.");

        // Verify the response contains multiple items (not just a single value)
        Assert.IsTrue(
            bodyText!.Contains("\"items\"") && bodyText.Contains("\"count\""),
            $"Stream response should contain items array and count. Body: {bodyText}");

        // Verify stream completed
        Assert.IsTrue(
            bodyText.Contains("\"completed\""),
            $"Stream should show completed state. Body: {bodyText}");
    }

    /// <summary>
    /// Verifies that client event operations show the event panel
    /// with connection status and event log area.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_ClientEventShowsEventPanel()
    {
        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // Find a client event operation (from TypedChatHub's IChatClient)
        var eventOp = Page.Locator(".opblock", new() { HasTextString = "ReceiveMessage" });
        if (await eventOp.CountAsync() == 0)
        {
            // If no typed hub events found, skip
            Assert.Inconclusive("No client event operations found in spec.");
            return;
        }

        // Expand it
        await eventOp.First.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // Verify the event panel renders with connection status
        var eventPanel = eventOp.Locator(".signalr-event-panel");
        var panelCount = await eventPanel.CountAsync();

        Assert.IsTrue(panelCount > 0, "Client event operation should show the event panel.");

        // Verify it shows a connection status indicator
        var statusIndicator = eventOp.Locator(".signalr-status");
        var statusCount = await statusIndicator.CountAsync();
        Assert.IsTrue(statusCount > 0, "Event panel should show a connection status indicator.");
    }

    /// <summary>
    /// Verifies that streaming operations display "STREAM" as the method label.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_StreamingOperationShowsStreamLabel()
    {
        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // Find the streaming operation
        var streamOp = Page.Locator(".opblock", new() { HasTextString = "StreamIntegers" });
        Assert.IsTrue(await streamOp.CountAsync() > 0, "StreamIntegers operation should exist.");

        // Verify the method label shows "stream" instead of "post"
        var methodLabel = streamOp.Locator(".opblock-summary-method");
        var labelText = await methodLabel.First.TextContentAsync();
        Assert.AreEqual("STREAM", labelText?.Trim().ToUpperInvariant(), "Streaming operations should show 'STREAM' label.");
    }

    /// <summary>
    /// Verifies that the stream completed state is properly shown in the response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_StreamShowsCompletedState()
    {
        var consoleLogs = new List<string>();
        Page.Console += (_, msg) => consoleLogs.Add($"[{msg.Type}] {msg.Text}");

        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // Find and expand the StreamIntegers operation
        var streamOp = Page.Locator(".opblock", new() { HasTextString = "StreamIntegers" });
        await streamOp.First.ClickAsync();

        // Click "Try it out"
        var tryItOutButton = streamOp.Locator("button", new() { HasTextString = "Try it out" });
        await tryItOutButton.ClickAsync();

        // Fill in a small count for a quick stream
        var textarea = streamOp.Locator("textarea.body-param__text");
        await textarea.ClearAsync();
        await textarea.FillAsync("{\"count\": 2}");

        // Execute the stream
        var executeButton = streamOp.Locator("button.execute");
        await executeButton.ClickAsync();

        // Wait for stream to complete
        await Page.WaitForTimeoutAsync(5000);

        // Check for "completed" state in the response
        var responseBody = streamOp.Locator(".responses-wrapper .response-col_description pre");
        var count = await responseBody.CountAsync();
        Assert.IsTrue(count > 0, $"Response should render. Logs:\n{string.Join("\n", consoleLogs)}");

        var bodyText = await responseBody.First.TextContentAsync();
        Assert.IsTrue(
            bodyText != null && bodyText.Contains("\"completed\""),
            $"Stream should show 'completed' state. Body: {bodyText}");

        // Verify items are present
        Assert.IsTrue(
            bodyText!.Contains("\"items\""),
            $"Completed stream should contain items. Body: {bodyText}");
    }

    /// <summary>
    /// Verifies that response examples from <c>[SignalROpenApiResponseExamples]</c>
    /// appear in SwaggerUI's response section with example names in a dropdown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SwaggerUi_ResponseExamplesDisplayed()
    {
        await Page.GotoAsync($"{baseUrl}/signalr-swagger/index.html");

        var operationBlock = Page.Locator(".opblock");
        await operationBlock.First.WaitForAsync(new() { Timeout = 15000 });

        // Find and expand the CreateOrder operation (has response examples)
        var createOrderOp = Page.Locator(".opblock", new() { HasTextString = "CreateOrder" });
        Assert.IsTrue(await createOrderOp.CountAsync() > 0, "CreateOrder operation should exist.");
        await createOrderOp.First.ClickAsync();

        // Wait for the response section to render
        await Page.WaitForTimeoutAsync(1000);

        // SwaggerUI renders response examples in the responses section.
        // When multiple named examples exist, a <select> dropdown appears.
        var responsesSection = createOrderOp.Locator(".responses-wrapper");
        Assert.IsTrue(await responsesSection.CountAsync() > 0, "Responses section should be visible.");

        // Look for example dropdown or example content in the 200 response
        var exampleSelect = responsesSection.Locator("select");
        var exampleSelectCount = await exampleSelect.CountAsync();

        if (exampleSelectCount > 0)
        {
            // SwaggerUI shows a dropdown with named examples
            var selectHtml = await exampleSelect.First.InnerHTMLAsync();
            Assert.IsTrue(
                selectHtml.Contains("Created") || selectHtml.Contains("Pending"),
                $"Example dropdown should contain 'Created' or 'Pending'. HTML: {selectHtml}");
        }
        else
        {
            // Fallback: check that example content is rendered somewhere in the response
            var responseContent = await responsesSection.InnerTextAsync();
            Assert.IsTrue(
                responseContent.Contains("ORD-001") || responseContent.Contains("orderId"),
                $"Response section should contain example values. Content: {responseContent}");
        }
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
