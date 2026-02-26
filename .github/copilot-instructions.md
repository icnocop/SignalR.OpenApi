# Copilot Instructions

## Repository Overview

This repository contains **SignalR.OpenApi**, a library providing OpenAPI 3.1 specification generation and SwaggerUI support for ASP.NET Core SignalR hubs.

### Solution Structure

- **`SignalR.OpenApi.slnx`** — Main solution containing all projects
- **`src/`** — Source projects (net8.0)
- **`test/`** — MSTest test projects
- **`samples/`** — Sample ASP.NET Core apps

### Core Projects

| Project | Purpose |
|---------|---------|
| `SignalR.OpenApi` | Core library: hub discovery (reflection), OpenAPI document generation, ASP.NET Core integration |
| `SignalR.OpenApi.FluentValidation` | FluentValidation rules → OpenAPI schema integration |
| `SignalR.OpenApi.SwaggerUi` | SwaggerUI plugin (JS/CSS) for interactive SignalR invocation |

### Key Technology Choices

- **Target Framework**: .NET 8 (primary)
- **OpenAPI Version**: 3.1
- **OpenAPI Generation**: `Microsoft.AspNetCore.OpenApi` with `IOpenApiDocumentTransformer`, `IOpenApiOperationTransformer`, `IOpenApiSchemaTransformer`
- **UI Layer**: `Swashbuckle.AspNetCore.SwaggerUI`
- **Testing**: MSTest
- **Code Style**: StyleCop Analyzers (latest preview) + IDisposableAnalyzers
- **Hub Discovery**: Reflection-based (runtime); source generator deferred to future
- **Validation**: FluentValidation integration for OpenAPI schema generation
- **Central Package Management**: `Directory.Packages.props`

## Build Commands

```shell
# Build the main solution
dotnet build SignalR.OpenApi.slnx

# Run tests (excludes Playwright E2E tests which require browser binaries)
dotnet test SignalR.OpenApi.slnx --filter "TestCategory!=Playwright"

# Run all tests including Playwright (requires: pwsh bin/Debug/net8.0/playwright.ps1 install)
dotnet test SignalR.OpenApi.slnx
```

## Key Patterns & Conventions

### Authentication
Leverage ASP.NET Core built-in support:
- `[Authorize]` attribute detection → `security` in OpenAPI spec → lock icon in SwaggerUI
- JWT Bearer: token passed via `accessTokenFactory` or query string (configurable)
- Windows Auth (Negotiate): `withCredentials: true` on HubConnection
- SwaggerUI's built-in Authorize dialog handles token input

### Hub Discovery
- **Reflection-based**: Scans assemblies for `Hub`/`Hub<TClient>` types at runtime
- Standard attributes for metadata: `[Tags]`, `[EndpointName]`, `[Authorize]`, `[ApiExplorerSettings(IgnoreApi = true)]`

### OpenAPI Document
- OpenAPI 3.1 specification
- Hub methods modeled as POST operations under `/hubs/{HubName}/{MethodName}`
- `x-signalr` vendor extension for SignalR-specific metadata (hub name, streaming flag, client events, parameter count, discriminator info)
- Separate document endpoint (default: `/openapi/signalr-v1.json`)
- Single complex object parameters are flattened (no wrapper property)
- Polymorphic types generate `oneOf`/`discriminator` schemas with OData-style sub-endpoints per derived type

### Request Body Schema Rules
- **Primitive parameters** (e.g., `string user, string message`): Each param is a property; supports both JSON and form-urlencoded
- **Single flat object** (e.g., `SendMessage(SendMessageRequest req)`): Properties flattened into body; supports both JSON and form-urlencoded
- **Polymorphic parameter** (e.g., `SendNotification(Notification n)`): Main endpoint uses `oneOf` (JSON-only); sub-endpoints per derived type support form-urlencoded
- **Multi-object parameters** (e.g., `Reply(ChatMessage orig, ChatMessage reply)`): Each param is a named property (JSON-only)

### Design Patterns
- Dependency Injection throughout (`IServiceCollection` extensions)
- Options pattern (`IOptions<SignalROpenApiOptions>`)
- Interface segregation (`IHubDiscoverer`, `ISignalROpenApiDocumentGenerator`)
- Central package management (`Directory.Packages.props`)
- Nullable reference types enabled
- XML documentation on all public APIs

### Important Implementation Details
- **System.Text.Json polymorphic deserialization** requires the type discriminator property to appear **first** in the JSON object. The SwaggerUI JS plugin handles this by reordering properties when injecting discriminator values.
- **SignalR `JsonHubProtocol` uses camelCase by default** — its `PayloadSerializerOptions` defaults to `JsonNamingPolicy.CamelCase`, which is **independent** of the OpenAPI document generator's `JsonSerializerOptions`. This means property names on the wire (e.g., `content`, `recipient`) differ from OpenAPI spec names when `PropertyNamingPolicy = null` (PascalCase: `Content`, `Recipient`). **Always use case-insensitive matching** when comparing property names across SignalR wire format and OpenAPI spec.
- **SignalR omits polymorphic type discriminators** — `JsonHubProtocol.WriteArguments` serializes with `argument.GetType()` (runtime type), bypassing `[JsonPolymorphic]` on the base type. Discriminators are only written when serializing as the declared base type. The JS plugin uses property-matching inference via `x-signalr.eventDiscriminators` metadata to determine and inject the type.
- **Swashbuckle plugin registration** uses `ConfigObject.Plugins` (not `InjectJavascript`) so the plugin is initialized with SwaggerUI, not after.
- **SwaggerUI component wrapping** — Props in `wrapComponents` may be **plain JS objects/arrays OR ImmutableJS** structures depending on the component and SwaggerUI version. Always use defensive access: `val.get ? val.get(0) : val[0]` for lists, `val.size != null ? val.size : val.length` for counts. The `parameters` component receives `pathMethod` (array/List) and `parameters` (ImmutableList). The "No parameters" section is hidden for SignalR operations since hub methods use request body, not URL parameters.
- **SwaggerUI form-urlencoded** values are wrapped as `{fieldName: {value: "val", errors: []}}` — the JS plugin unwraps these and coerces types (string→number/boolean).
- **Playwright tests** require browser binaries installed separately; they are excluded from CI via `[TestCategory("Playwright")]`.
