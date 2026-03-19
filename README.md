# SignalR.OpenApi

[![Build](https://github.com/icnocop/SignalR.OpenApi/actions/workflows/build.yml/badge.svg)](https://github.com/icnocop/SignalR.OpenApi/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/SignalR.OpenApi.svg)](https://www.nuget.org/packages/SignalR.OpenApi)

OpenAPI 3.1 specification generation and SwaggerUI support for ASP.NET Core SignalR hubs.

## Features

- Generates OpenAPI 3.1 specifications from SignalR hub methods
- Interactive SwaggerUI with SignalR protocol invocation (no HTTP — real SignalR calls)
- **Streaming support**: `IAsyncEnumerable<T>` and `ChannelReader<T>` with accumulated item history and stream state tracking
- **Client event monitoring**: Auto-subscribes to typed hub (`Hub<TClient>`) events with real-time event log panel
- Supports standard ASP.NET Core attributes (`[Authorize]`, `[Tags]`, `[EndpointSummary]`, `[Obsolete]`, etc.)
- Document-level tag definitions with descriptions (from options or XML summary fallback)
- XML documentation comments for descriptions and examples
- `[JsonPolymorphic]` / `[JsonDerivedType]` polymorphic schema support with OData-style sub-endpoints
- Data Annotation validation attributes mapped to OpenAPI schema constraints
- FluentValidation rules mapped to OpenAPI schema constraints
- Security scheme detection from `[Authorize]` / `[AllowAnonymous]`
- JWT Bearer token support in SwaggerUI (header or query string)
- Custom HTTP headers (static or user-enterable via SwaggerUI Authorize dialog)
- Connection status indicator with automatic reconnection handling
- Form-urlencoded input mode for primitive and flat object parameters
- Multiple named request/response examples via custom attributes
- Enum schema generation (integer or string based on `JsonStringEnumConverter`)
- Embedded `@microsoft/signalr` bundle (no CDN dependency)
- Zero proprietary attributes required for core functionality

## Getting Started

### Installation

```shell
dotnet add package SignalR.OpenApi
dotnet add package SignalR.OpenApi.SwaggerUi
```

### Usage

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSignalROpenApi();
builder.Services.AddSignalRSwaggerUi();

var app = builder.Build();

app.MapHub<ChatHub>("/hubs/chat");
app.MapSignalROpenApi();
app.UseSignalRSwaggerUi();

app.Run();
```

The OpenAPI specification is served at `/openapi/signalr-v1.json`.
The SwaggerUI is available at `/signalr-swagger`.

### Configuration

```csharp
builder.Services.AddSignalROpenApi(options =>
{
    options.DocumentTitle = "My SignalR API";
    options.DocumentVersion = "v1";

    // Include type discriminator in JSON examples for polymorphic sub-endpoints (default: true)
    options.IncludeDiscriminatorInExamples = true;

    // Add descriptions for tags (displayed as group descriptions in SwaggerUI)
    options.TagDescriptions["Chat"] = "Real-time chat operations";
    options.TagDescriptions["Chat Events"] = "Server-to-client chat notifications";

    // Configure JSON property naming (default: camelCase)
    options.JsonSerializerOptions = new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    };

    // User-enterable headers shown in the SwaggerUI Authorize dialog.
    // Each entry appears as an apiKey security scheme (in: header) so users
    // can enter a value at runtime before invoking hub methods.
    options.ApiKeyHeaders["X-Custom-Header"] = "A custom header sent with every hub connection.";
});

builder.Services.AddSignalRSwaggerUi(options =>
{
    options.RoutePrefix = "signalr-swagger";    // SwaggerUI route (default)
    options.SpecUrl = "/openapi/signalr-v1.json"; // Spec endpoint (default)
    options.DocumentTitle = "SignalR API";       // Browser tab title (default)
    options.StripAsyncSuffix = true;             // Strip "Async" from display names (default)
    options.SyntaxHighlight = true;              // Enable syntax highlighting (default)
    options.DefaultModelsExpandDepth = -1;       // Hide models section (default), 1 to show
    options.DocExpansion = DocExpansion.List;     // Tag expand mode: List (default), Full, None
    options.SortTagsAlphabetically = false;      // Sort tags A-Z (default: document order)
    options.SortOperationsAlphabetically = false; // Sort operations A-Z (default: document order)

    // Static headers sent with every SignalR hub connection
    options.Headers["X-Custom-Header"] = "MyValue";
});
```

## SwaggerUI Features

### Method Labels

SignalR operations display custom method labels in SwaggerUI:

| Label | Description |
|-------|-------------|
| **INVOKE** | Standard hub method invocation |
| **STREAM** | Streaming method (`IAsyncEnumerable<T>` / `ChannelReader<T>`) |
| **EVENT** | Client event from typed hub (`Hub<TClient>`) |

### Streaming

Streaming operations accumulate items into a growing response array as they arrive. The response shows:

```json
{
  "state": "streaming",
  "count": 5,
  "items": [10, 9, 8, 7, 6]
}
```

When the stream completes, the state changes to `"completed"`. If an error occurs, it shows `"error: ..."`. A **Stop Stream** button appears while streaming is active to cancel the subscription.

### Client Events

Client events (from `Hub<TClient>` interface methods) appear as **EVENT** operations. When you expand one, an event log panel shows:

- **Connection status**: Connected / Disconnected indicator
- **Connect & Listen**: Button to establish hub connection and start receiving events
- **Event log**: Real-time list of received events with timestamps and JSON payloads
- **Clear Log**: Button to reset the event history

Events are automatically subscribed when connecting to a hub via any invoke or stream operation.

### Request Body Input Modes

Hub methods with parameters support two input modes, selectable via a content-type dropdown in SwaggerUI:

| Content Type | Input Mode | Available When |
|---|---|---|
| `application/json` | Raw JSON textarea | Always |
| `application/x-www-form-urlencoded` | Individual form fields | Primitive params, single flat objects, polymorphic sub-endpoints |

The dropdown appears above the request body when you click **Try it out**. Form field values are automatically coerced to the correct type (e.g., `"5"` → `5` for integers, `"true"` → `true` for booleans).

> **Note**: Methods with multi-object parameters (two or more complex objects) only support `application/json` because SwaggerUI cannot render nested objects as form fields.

### Polymorphic Parameters

Parameters using `[JsonPolymorphic]` / `[JsonDerivedType]` are supported via two mechanisms:

1. **Main endpoint** (`/hubs/Chat/SendNotification`) — uses `oneOf` with a `discriminator` for the type selector dropdown in JSON mode.
2. **OData-style sub-endpoints** (`/hubs/Chat/SendNotification/text`, `/hubs/Chat/SendNotification/alert`) — each derived type gets its own endpoint with a flat schema, supporting both JSON and form-urlencoded input.

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextNotification), "text")]
[JsonDerivedType(typeof(AlertNotification), "alert")]
public abstract class Notification
{
    public required string Recipient { get; set; }
}

public class TextNotification : Notification
{
    public required string Content { get; set; }
}

public class AlertNotification : Notification
{
    public required string Title { get; set; }
    public required string Severity { get; set; }
}
```

> **Note**: `System.Text.Json` polymorphic deserialization requires the type discriminator property to appear **first** in the JSON object. The SwaggerUI plugin handles this automatically for sub-endpoints.
>
> By default, `IncludeDiscriminatorInExamples = true` makes the discriminator visible in JSON request examples but hidden in form-urlencoded inputs. Set to `false` to hide the discriminator from all examples (the plugin still injects it at invocation time).

## Supported Attributes

| Attribute | OpenAPI Mapping |
|-----------|----------------|
| `[Tags("group")]` | `tags` on operation + document-level tag definition |
| `[EndpointSummary("...")]` | `summary` on operation |
| `[EndpointDescription("...")]` | `description` on operation |
| `[EndpointName("Name")]` | `operationId` on operation |
| `[Description("...")]` | `description` on parameter/property |
| `[Authorize]` / `[AllowAnonymous]` | `security` requirement |
| `[ApiExplorerSettings(IgnoreApi = true)]` | Excluded from spec |
| `[ExcludeFromDescription]` | Excluded from spec |
| `[Produces("application/json")]` | Response content type |
| `[Obsolete]` | `deprecated: true` |
| `[JsonPolymorphic]` / `[JsonDerivedType]` | `discriminator` / `oneOf` with sub-endpoints |
| `[Required]`, `[StringLength]`, `[Range]` | Schema constraints |
| XML `<summary>`, `<param>`, `<returns>` | Descriptions |
| XML `<example>` | Example values |
| `[SignalROpenApiRequestExamples]` | Named request examples |
| `[SignalROpenApiResponseExamples]` | Named response examples |

## Tag Grouping

Operations are grouped by tags in SwaggerUI. The generator automatically collects all unique tags from operations and adds them to the document-level `tags` section.

### Tag Descriptions

Tag descriptions appear as group headers in SwaggerUI. There are two ways to provide them:

**1. Via options** (explicit):

```csharp
builder.Services.AddSignalROpenApi(options =>
{
    options.TagDescriptions["Chat"] = "Real-time chat operations";
    options.TagDescriptions["Admin"] = "Administrative hub methods";
});
```

**2. Via XML summary** (automatic fallback): When a tag name matches the hub name (e.g., tag `"Chat"` matches `ChatHub`), the hub's XML `<summary>` is used as the tag description automatically.

```csharp
/// <summary>
/// Real-time chat operations.
/// </summary>
public class ChatHub : Hub
{
    // Methods default to the "Chat" tag → description comes from XML summary above
}
```

Explicit `TagDescriptions` always take precedence over XML summary fallback.

### Tag Sources

| Source | Tag Name |
|--------|----------|
| Hub class (default) | Hub name without `Hub` suffix (e.g., `ChatHub` → `"Chat"`) |
| `[Tags("group")]` on hub class or method | Specified tag name |
| Client events (`Hub<TClient>`) | `"{HubName} Events"` (e.g., `"Chat Events"`) |

## Request Body Schema

Hub method parameters are mapped to the OpenAPI request body schema:

- **Single complex object parameter** (e.g., `SendMessage(SendMessageRequest request)`): The object's properties are **flattened** directly into the request body — no wrapper property.
- **Multiple parameters** (e.g., `SendMessage(string user, string message)` or `Reply(ChatMessage original, ChatMessage reply)`): Each parameter becomes a named property in a wrapper object.
- **Primitive parameters** (e.g., `string`, `int`): Always wrapped with the parameter name as the property key.

### Response Codes

- **204 No Content**: Hub methods returning `void` or `Task` (no return value)
- **200 OK**: Hub methods returning `Task<T>` or streaming results

## Examples

Provide multiple named examples for request and response bodies using custom attributes and the `ISignalROpenApiExamplesProvider<T>` interface.

### 1. Define a request model and example provider

```csharp
public class SendMessageRequest
{
    public string User { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class SendMessageExamplesProvider : ISignalROpenApiExamplesProvider<SendMessageRequest>
{
    public IEnumerable<SignalROpenApiExample<SendMessageRequest>> GetExamples()
    {
        yield return new SignalROpenApiExample<SendMessageRequest>(
            "Greeting",
            new SendMessageRequest { User = "Alice", Message = "Hello, everyone!" })
        {
            Summary = "A friendly greeting",
        };

        yield return new SignalROpenApiExample<SendMessageRequest>(
            "Question",
            new SendMessageRequest { User = "Bob", Message = "What time is the meeting?" })
        {
            Summary = "Asking a question",
        };
    }
}
```

### 2. Apply the attribute to a hub method

```csharp
[SignalROpenApiRequestExamples(typeof(SendMessageExamplesProvider))]
public async Task SendMessage(string user, string message)
{
    await Clients.All.ReceiveMessage(user, message);
}
```

The examples appear in SwaggerUI's example dropdown for the request body. Response examples work the same way using `[SignalROpenApiResponseExamples]`.

Example providers are resolved from DI first, with `Activator.CreateInstance` as a fallback.

## FluentValidation Integration

The `SignalR.OpenApi.FluentValidation` package automatically maps FluentValidation rules to OpenAPI schema constraints.

> **Note:** FluentValidation applies to **complex object parameters** only (classes with properties). Hub methods with primitive parameters (e.g., `string user, string message`) are not validated — use a request object instead (e.g., `SendMessage(SendMessageRequest request)`). When a hub method has a single complex object parameter, the schema is **flattened** — the object's properties appear directly in the request body without a wrapper property.

### Setup

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<MyValidator>();
builder.Services.AddSignalROpenApi();
builder.Services.AddSignalRFluentValidation();
```

### Supported Rules

| FluentValidation Rule | OpenAPI Schema |
|----------------------|----------------|
| `NotNull()` / `NotEmpty()` | `required`, `nullable: false` |
| `NotEmpty()` (string) | `minLength: 1` |
| `Length(min, max)` / `MaximumLength(n)` | `minLength`, `maxLength` |
| `Matches(regex)` | `pattern` |
| `GreaterThan(n)` | `minimum` + `exclusiveMinimum` |
| `GreaterThanOrEqualTo(n)` | `minimum` |
| `LessThan(n)` | `maximum` + `exclusiveMaximum` |
| `LessThanOrEqualTo(n)` | `maximum` |
| `InclusiveBetween(from, to)` | `minimum`, `maximum` |
| `ExclusiveBetween(from, to)` | `minimum`, `maximum` + exclusive flags |
| `EmailAddress()` | `pattern` (email regex) |

Validators are resolved from DI via `IValidator<T>`. Nested child validators are supported.

## Custom Headers

Custom HTTP headers can be sent with every SignalR hub connection. Two approaches are available depending on whether the value is known at startup or entered by the user at runtime.

### Static Headers

Use `SignalRSwaggerUiOptions.Headers` to configure headers with fixed values. These are included in the negotiate request and all HTTP-based transports (long-polling, server-sent events). WebSocket connections carry them on the initial upgrade request.

```csharp
builder.Services.AddSignalRSwaggerUi(options =>
{
    options.Headers["X-Custom-Header"] = "MyValue";
});
```

### User-Enterable Headers (Authorize Dialog)

Use `SignalROpenApiOptions.ApiKeyHeaders` to define headers that appear in SwaggerUI's **Authorize** dialog. Each entry is rendered as an `apiKey` security scheme (`in: header`) in the OpenAPI document. Users can enter values at runtime before invoking hub methods.

```csharp
builder.Services.AddSignalROpenApi(options =>
{
    options.ApiKeyHeaders["X-Custom-Header"] = "A custom header sent with every hub connection.";
});
```

When the user clicks the **Authorize** button in SwaggerUI, they see an input field for each configured header. The entered values are automatically included on every SignalR hub connection.

| Approach | Where configured | User can change at runtime? |
|----------|------------------|-----------------------------|
| `SignalRSwaggerUiOptions.Headers` | Static value at startup | No |
| `SignalROpenApiOptions.ApiKeyHeaders` | Authorize dialog input | Yes |

Both approaches can be combined — static headers provide defaults while apiKey headers allow user overrides. When both define the same header name, the user-entered value from the Authorize dialog takes precedence.

## Enum Schema Generation

Enum types are automatically mapped to OpenAPI schemas. The schema format depends on whether a `JsonStringEnumConverter` is configured:

| Converter | Schema Type | Example Values |
|-----------|-------------|----------------|
| None (default) | `integer` | `0`, `1`, `2` |
| `JsonStringEnumConverter` | `string` with `enum` | `"Pending"`, `"Active"`, `"Completed"` |

The converter is detected from `JsonSerializerOptions.Converters` (global) or `[JsonConverter]` on the enum type.

```csharp
// Global: all enums serialize as strings
options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

// Per-type: only this enum serializes as strings
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Status { Pending, Active, Completed }
```

## Packages

| Package | Description |
|---------|-------------|
| [SignalR.OpenApi](https://www.nuget.org/packages/SignalR.OpenApi) | Core library: hub discovery, OpenAPI generation |
| [SignalR.OpenApi.FluentValidation](https://www.nuget.org/packages/SignalR.OpenApi.FluentValidation) | FluentValidation rules → OpenAPI schema constraints |
| [SignalR.OpenApi.SwaggerUi](https://www.nuget.org/packages/SignalR.OpenApi.SwaggerUi) | SwaggerUI with interactive SignalR invocation, streaming, and event monitoring |

## Related Projects

The following open-source projects also provide OpenAPI, SwaggerUI, or developer tooling for ASP.NET Core SignalR hubs. SignalR.OpenApi was designed with awareness of these projects and aims to combine the best aspects of each.

| Feature                       | **SignalR.OpenApi**                                                | [SigSpec](https://github.com/RicoSuter/SigSpec) | [SignalRSwaggerGen](https://github.com/essencebit/SignalRSwaggerGen) | [TypedSignalR.Client.DevTools](https://github.com/nenoNaninu/TypedSignalR.Client.DevTools) | [NSwag4SignalR](https://github.com/ben-voss/NSwag4SignalR) |
| ----------------------------- | ------------------------------------------------------------------ | ----------------------------------------------- | -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ | ---------------------------------------------------------- |
| **OpenAPI spec generation**   | ✅ 3.1                                                              | ✅ Custom (SigSpec)                              | ✅ Swagger 2.0 / OAS 3.0                                              | ✅ Custom (spec.json)                                                                       | ✅ 3.0                                                      |
| **OpenAPI library**           | `Microsoft.AspNetCore.OpenApi`                                     | Custom                                          | Swashbuckle `IDocumentFilter`                                        | Custom                                                                                     | NSwag `IDocumentProcessor`                                 |
| **Interactive UI**            | ✅ SwaggerUI (Swashbuckle)                                          | ❌                                               | ❌ Spec only                                                          | ✅ Custom (Next.js / Bulma)                                                                 | ✅ SwaggerUI (NSwag)                                        |
| **Real SignalR invocation**   | ✅ `@microsoft/signalr`                                             | ❌                                               | ❌                                                                    | ✅ `@microsoft/signalr`                                                                     | ✅ `@microsoft/signalr`                                     |
| **Streaming UI**              | ✅ Accumulated history, state tracking, stop button                 | ❌                                               | ❌                                                                    | ✅ Server-to-client & client-to-server                                                      | ✅ PUT operations                                           |
| **Client event monitoring**   | ✅ Real-time event log panel                                        | ❌                                               | ❌                                                                    | ✅ Event subscription                                                                       | ✅ GET operations                                           |
| **Hub discovery**             | Reflection                                                         | Reflection                                      | Attribute-based (`[SignalRHub]`)                                     | Source generator (`MapHub<T>()`)                                                           | Endpoint metadata (`HubMetadata`)                          |
| **FluentValidation → schema** | ✅                                                                  | ❌                                               | ❌                                                                    | ❌                                                                                          | ❌                                                          |
| **Polymorphic types**         | ✅ `oneOf`/`discriminator` + sub-endpoints                          | ❌                                               | ❌                                                                    | ❌                                                                                          | ❌                                                          |
| **Form-urlencoded input**     | ✅ Flat params & objects                                            | ❌                                               | ❌                                                                    | ❌                                                                                          | ❌                                                          |
| **Named examples**            | ✅ Custom attributes + providers                                    | ❌                                               | ❌                                                                    | ❌                                                                                          | ❌                                                          |
| **Auth (JWT / Windows)**      | ✅ Built-in SwaggerUI                                               | ❌                                               | ✅ `[Authorize]` detection                                            | ✅ `accessTokenFactory`                                                                     | ❌                                                          |
| **Custom headers**            | ✅ Static + Authorize dialog                                        | ❌                                               | ❌                                                                    | ❌                                                                                          | ❌                                                          |
| **Tag descriptions**          | ✅ Options + XML summary fallback                                   | ❌                                               | Partial (tags only)                                                  | ❌                                                                                          | ❌                                                          |
| **Enum schema**               | ✅ Integer + `JsonStringEnumConverter`                               | ❌                                               | Partial (via Swashbuckle)                                            | ❌                                                                                          | ✅ (via NSwag)                                               |
| **Standard attributes**       | ✅ `[Tags]`, `[EndpointSummary]`, `[Authorize]`, `[Obsolete]`, etc. | Partial                                         | ✅ `[Authorize]`, custom                                              | Partial                                                                                    | Partial                                                    |
| **Target framework**          | .NET 8+                                                            | .NET Core 3.1+                                  | .NET 5+                                                              | .NET 6+                                                                                    | .NET 10                                                    |
| **NuGet packages**            | 3 packages                                                         | ❌                                               | ✅                                                                    | ✅                                                                                          | ✅                                                          |

### Other related projects

| Project | Description |
|---------|-------------|
| [nswag-fluentvalidation](https://github.com/zymlabs/nswag-fluentvalidation) (ZymLabs) | FluentValidation → OpenAPI schema mapping for NSwag; rule-based architecture pattern |
| [Swashbuckle.AspNetCore.Filters](https://github.com/mattfrear/Swashbuckle.AspNetCore.Filters) | Request/response examples and security filters for Swashbuckle |
| [Signalr.Hubs.TypeScriptGenerator](https://github.com/geniussportsgroup/Signalr.Hubs.TypeScriptGenerator) | TypeScript type generation from SignalR hubs (legacy .NET Framework / SignalR 2.x) |

## License

MIT
