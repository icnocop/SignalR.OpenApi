// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using FluentValidation;
using SignalR.OpenApi.Extensions;
using SignalR.OpenApi.Sample.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
builder.Services.AddValidatorsFromAssemblyContaining<SendMessageRequestValidator>();
builder.Services.AddSignalROpenApi(options =>
{
    options.DocumentTitle = "SignalR.OpenApi Sample";
    options.DocumentVersion = "v1";
    options.IncludeDiscriminatorInExamples = true;

    // Tag descriptions shown as group headers in SwaggerUI
    options.TagDescriptions["Chat"] = "Real-time chat operations including messaging, notifications, and streaming.";
    options.TagDescriptions["Groups"] = "Send messages to specific SignalR groups.";
    options.TagDescriptions["Notifications"] = "Polymorphic notification delivery (text and alert types).";
    options.TagDescriptions["Streaming"] = "Server-to-client streaming operations.";
    options.TagDescriptions["Chat Events"] = "Server-to-client callbacks. Subscribe to receive real-time notifications.";

    // Default: camelCase (matches ASP.NET Core default)
    // For PascalCase:
    options.JsonSerializerOptions.PropertyNamingPolicy = null;

    // User-enterable headers shown in the SwaggerUI Authorize dialog.
    // Each entry appears as an apiKey security scheme (in: header) so users
    // can enter a value at runtime before invoking hub methods.
    options.ApiKeyHeaders["X-Custom-Header"] = "A custom header sent with every hub connection.";
});
builder.Services.AddSignalRFluentValidation();
builder.Services.AddSignalRSwaggerUi(options =>
{
    options.StripAsyncSuffix = true;

    // Custom headers sent with every SignalR hub connection.
    // These are included in the negotiate request and all HTTP-based transports.
    options.Headers["X-Custom-Header"] = "MyValue";
});

var app = builder.Build();

app.MapHub<ChatHub>("/hubs/chat");
app.MapSignalROpenApi();
app.UseSignalRSwaggerUi();

app.Run();
