// Copyright (c) SignalR.OpenApi Contributors. Licensed under the MIT License.

using FluentValidation;
using SignalR.OpenApi.Extensions;
using SignalR.OpenApi.Sample.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
builder.Services.AddValidatorsFromAssemblyContaining<SendMessageRequestValidator>();
builder.Services.AddSignalROpenApi(options =>
{
    options.DocumentTitle = "SignalR.OpenApi Sample";
    options.DocumentVersion = "v1";
});
builder.Services.AddSignalRFluentValidation();
builder.Services.AddSignalRSwaggerUi();

var app = builder.Build();

app.MapHub<ChatHub>("/hubs/chat");
app.MapSignalROpenApi();
app.UseSignalRSwaggerUi();

app.Run();
