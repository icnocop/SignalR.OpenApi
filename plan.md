# SignalR.OpenApi – Implementation Plan

## Problem Statement

ASP.NET Core SignalR hubs lack first-class OpenAPI/SwaggerUI support. This project creates a library that generates OpenAPI specifications from SignalR hubs and provides an interactive SwaggerUI experience for invoking hub methods, visualizing streams, and monitoring client events.

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Target Framework | .NET 8 first | Broad adoption, LTS |
| OpenAPI Generation | `Microsoft.AspNetCore.OpenApi` | Built-in, uses transformers (`IOpenApiDocumentTransformer`, `IOpenApiOperationTransformer`, `IOpenApiSchemaTransformer`, `IOpenApiDocumentProvider`) |
| Hub Discovery | Reflection-based (Phases 1–5); source generator deferred to Future | Reflection is simpler to start; source gen adds complexity and can be layered on later |
| UI Layer | `Swashbuckle.AspNetCore.SwaggerUI` | Most popular, supports JS/CSS injection, OAuth2 |
| Spec Delivery | Separate SignalR document at `/openapi/signalr-v1.json` (Phases 1–5); injection into existing doc (Future) |
| OpenAPI Version | OpenAPI 3.1 | Latest standard; supported by `Microsoft.AspNetCore.OpenApi` on .NET 8+ |
| Validation | FluentValidation integration for OpenAPI schema generation via `Microsoft.AspNetCore.OpenApi` |
| Testing | MSTest | Project standard |
| Code Style | Latest StyleCop Analyzers (preview) + IDisposableAnalyzers | Enforced across all projects |
| SignalR JS Client | `@microsoft/signalr` embedded bundle (not CDN) | No external dependency |
| Phased Delivery | Each phase completed and approved before next begins |
| Custom Attributes | Minimal — use standard ASP.NET Core attributes; custom example attributes added in Phase 4 |
| Extensibility | Framework allows users to add custom attribute handlers via DI |
| Deliverables | NuGet packages published to NuGet.org |
| MVP Release | Stable v1.0 after Phase 3 (core + SwaggerUI + streaming/events) |

## Attribute Support Matrix

### ✅ Supported Standard Attributes

| Attribute | OpenAPI Mapping | Notes |
|-----------|----------------|-------|
| `[Tags("group")]` | `tags` on operation | Groups hub methods in SwaggerUI |
| `[EndpointSummary("...")]` | `summary` on operation | Short description |
| `[EndpointDescription("...")]` | `description` on operation | Detailed description |
| `[EndpointName("Name")]` | `operationId` on operation | Rename method in spec (e.g., strip `Async` suffix) |
| `[Description("...")]` | `description` on parameter/property | System.ComponentModel.DescriptionAttribute |
| `[Authorize]` / `[AllowAnonymous]` | `security` requirement + `securitySchemes` | Lock icon in SwaggerUI |
| `[ApiExplorerSettings(IgnoreApi = true)]` | Excluded from spec | Hides hub or method |
| `[ExcludeFromDescription]` | Excluded from spec | ASP.NET Core 8+ equivalent of above |
| `[Produces("application/json")]` | `responses` content type | Response content type |
| `[Obsolete]` | `deprecated: true` | Marks operation as deprecated |
| `[JsonPolymorphic]` / `[JsonDerivedType]` | `discriminator` / `oneOf` in schema | System.Text.Json polymorphic types |
| XML docs `<summary>` | `summary` / `description` | Method and parameter descriptions |
| XML docs `<param>` | Parameter `description` | Parameter-level docs |
| XML docs `<returns>` | Response `description` | Return type docs |
| XML docs `<example>` | `example` in schema | Example values |
| Data Annotations (`[Required]`, `[StringLength]`, `[Range]`, etc.) | Schema `required`, `minLength`, `maxLength`, `minimum`, `maximum` | Standard validation attributes |

### ❌ Not Supported (documented with rationale)

| Attribute | Reason |
|-----------|--------|
| `[FromQuery]`, `[FromRoute]`, `[FromHeader]`, `[FromForm]`, `[FromBody]` | SignalR doesn't use HTTP parameter binding; all arguments are sent as a serialized array via the SignalR protocol |
| `[ControllerName]` | SignalR hubs aren't controllers; hub name derives from the type name or `MapHub<T>()` path |
| `[ActionName]` | Use `[EndpointName]` instead for method rename |
| `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]` | SignalR invocations aren't HTTP requests; all operations are modeled as POST in OpenAPI because hub invocations are always request/response. The SwaggerUI plugin uses SignalR protocol regardless of the HTTP verb shown. |

### Custom Attributes (Phase 4)

| Attribute | Purpose | Design |
|-----------|---------|--------|
| `[SignalROpenApiRequestExamples(Type examplesProviderType)]` | Multiple named request examples per method | `AllowMultiple = true`, uses `ISignalROpenApiExamplesProvider<T>` |
| `[SignalROpenApiResponseExamples(Type examplesProviderType)]` | Multiple named response examples per method | Same pattern |

Consolidated example provider interface:
```csharp
public interface ISignalROpenApiExamplesProvider<T>
{
    IEnumerable<SignalROpenApiExample<T>> GetExamples();
}

public class SignalROpenApiExample<T>
{
    public required string Name { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public required T Value { get; set; }
}
```

## Project Structure

```
SignalR.OpenApi.slnx
├── src/
│   ├── SignalR.OpenApi/                            # Core library: models, hub discovery, OpenAPI generation
│   ├── SignalR.OpenApi.FluentValidation/            # FluentValidation → OpenAPI schema integration
│   └── SignalR.OpenApi.SwaggerUi/                   # SwaggerUI plugin (JS/CSS) for SignalR-specific panels
├── test/
│   ├── SignalR.OpenApi.Tests/                       # Core library unit tests (MSTest)
│   └── SignalR.OpenApi.FluentValidation.Tests/      # FluentValidation integration tests (MSTest)
├── samples/
│   └── SignalR.OpenApi.Sample/                      # Sample ASP.NET Core app demonstrating all features
├── .github/
│   └── workflows/
│       └── build.yml                                # CI/CD: build, test, pack, publish to NuGet.org
└── README.md                                        # Getting started guide
```

> **Note**: `SignalR.OpenApi.SourceGenerator`, document injection mode, and advanced UI features are deferred to Future.

### Project Responsibilities

#### `SignalR.OpenApi` (Core)
- **Hub metadata model**: `SignalRHubInfo`, `SignalRMethodInfo`, `SignalRParameterInfo`, `SignalRClientEventInfo`
- **Reflection-based hub discoverer**: Scans assemblies for `Hub`/`Hub<T>` types, extracts methods, parameters, return types, streaming detection, client callbacks
- **OpenAPI document generator**: Converts hub metadata → `OpenApiDocument` using `Microsoft.AspNetCore.OpenApi`
- **ASP.NET Core integration**: `AddSignalROpenApi()` / `MapSignalROpenApi()` extension methods
- **Transformers**: `IOpenApiDocumentTransformer`, `IOpenApiOperationTransformer`, `IOpenApiSchemaTransformer` for SignalR-specific customization (`x-signalr` vendor extension)
- **Standard attribute support**: `[Tags]`, `[EndpointSummary]`, `[EndpointDescription]`, `[EndpointName]`, `[Description]`, `[Authorize]`, `[AllowAnonymous]`, `[ApiExplorerSettings]`, `[ExcludeFromDescription]`, `[Produces]`, `[Obsolete]`, `[JsonPolymorphic]`, `[JsonDerivedType]`, Data Annotations
- **Extensibility**: Users can register custom `IOpenApiOperationTransformer` or `IOpenApiSchemaTransformer` implementations to handle their own custom attributes
- **Configuration**: `SignalROpenApiOptions` for route templates, document name, assembly scanning, hub filtering
- **Security scheme detection**: Reads `[Authorize]` from hubs/methods, adds `securitySchemes` to doc
- **XML doc comments**: Extracts `<summary>`, `<param>`, `<returns>`, `<example>` for descriptions and examples
- **Polymorphism**: Supports `[JsonPolymorphic]`/`[JsonDerivedType]` → `discriminator`/`oneOf` in OpenAPI schema, with OData-style sub-endpoints per derived type
- **DRY / SRP**: Each class has a single responsibility; shared logic extracted into reusable services
- **NuGet packaging**: `.csproj` configured with package metadata for NuGet.org publishing

#### `SignalR.OpenApi.FluentValidation`
- **`IOpenApiSchemaTransformer`**: Applies FluentValidation rules to OpenAPI schemas for hub method parameter types
- **Rule mappings**: `Required`, `NotEmpty`, `Length`, `Pattern`, `Range`, `Between`, `Email` → OpenAPI `required`, `minLength`, `maxLength`, `pattern`, `minimum`, `maximum`
- **DI integration**: Resolves `IValidator<T>` from service provider
- **Built on**: `Microsoft.AspNetCore.OpenApi` schema transformers

#### `SignalR.OpenApi.SwaggerUi`
- **SwaggerUI plugin (JavaScript)**: Overrides default HTTP execution for SignalR operations, registered via `ConfigObject.Plugins`
- **Panels**: Connection panel, event log, stream results
- **Connection management**: Creates `HubConnection` via `@microsoft/signalr` (embedded bundle, not CDN)
- **Auth UI**: JWT token input (reuse SwaggerUI's built-in Authorize dialog), token delivery via header or query string
- **Form input**: Form-urlencoded support for primitive and flat object parameters with automatic type coercion
- **Polymorphic support**: Discriminator injection (property-first ordering required by System.Text.Json) for sub-endpoint form submissions
- **CSS**: Matches SwaggerUI look and feel
- **Embedded resources**: JS/CSS/signalr.js bundled as embedded resources, served via middleware
- **Extension method**: `UseSignalRSwaggerUi()` injects the plugin into Swashbuckle's SwaggerUI

## NuGet Packages Produced

| Package ID | Contents | Phase |
|------------|----------|-------|
| `SignalR.OpenApi` | Core library: hub discovery, OpenAPI generation, ASP.NET Core integration | 1 |
| `SignalR.OpenApi.SwaggerUi` | SwaggerUI plugin with embedded `@microsoft/signalr` bundle | 2 |
| `SignalR.OpenApi.FluentValidation` | FluentValidation → OpenAPI schema transformer | 5 |

## NuGet Package Dependencies

| Package | Used By | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.OpenApi` | SignalR.OpenApi | OpenAPI document generation, transformers |
| `Swashbuckle.AspNetCore.SwaggerUI` | SignalR.OpenApi.SwaggerUi | SwaggerUI hosting |
| `Microsoft.AspNetCore.SignalR.Common` | SignalR.OpenApi | SignalR hub base types |
| `FluentValidation` | SignalR.OpenApi.FluentValidation | Validation rule discovery |
| `FluentValidation.DependencyInjectionExtensions` | SignalR.OpenApi.FluentValidation | DI validator resolution |
| `StyleCop.Analyzers` | All projects | Code style enforcement (latest preview) |
| `IDisposableAnalyzers` | All projects | Disposable pattern analysis |
| `Microsoft.NET.Test.Sdk` | Test projects | Test infrastructure |
| `MSTest.TestAdapter` | Test projects | MSTest adapter |
| `MSTest.TestFramework` | Test projects | MSTest framework |

---

## Phases

> **Release strategy**: Phases 1–3 = **MVP (stable v1.0 release)**. Phases 4–5 = post-v1.0 enhancements. Future items = community contributions welcome.

---

### Phase 1 – Core Hub Discovery & OpenAPI Generation ✅

**Goal**: Generate a valid OpenAPI document from SignalR hubs using reflection, served at a dedicated endpoint. Only standard ASP.NET Core attributes and XML docs — no custom attributes.

- [x] Create solution structure (`src/`, `test/`, `samples/` folders)
- [x] Add all projects to `SignalR.OpenApi.slnx`
- [x] Set up `Directory.Build.props` with common settings (net8.0, LangVersion preview, Nullable enable, ImplicitUsings, StyleCop Analyzers, IDisposableAnalyzers, TreatWarningsAsErrors, GenerateDocumentationFile)
- [x] Set up `Directory.Packages.props` for central package management
- [x] Create `README.md` at repository root
- [x] Create `.github/workflows/build.yml` (CI: build, test, pack on push/PR; CD: auto-publish to NuGet.org on release tag `v*`)
- [x] **`SignalR.OpenApi` project** (net8.0 class library):
  - [x] Configure `.csproj` with NuGet package metadata
  - [x] Hub metadata models: `SignalRHubInfo`, `SignalRMethodInfo`, `SignalRParameterInfo`, `SignalRClientEventInfo`
  - [x] `IHubDiscoverer` interface + `ReflectionHubDiscoverer` implementation
    - [x] Discover `Hub` and `Hub<TClient>` types from configured assemblies
    - [x] Extract public methods (filter out base `Hub` methods)
    - [x] Detect return types: `Task`, `Task<T>`, `IAsyncEnumerable<T>`, `ChannelReader<T>`
    - [x] Mark streaming methods (`IsStreaming = true`)
    - [x] Extract client events from `TClient` interface on typed hubs
    - [x] Read all supported standard attributes
    - [x] Read XML doc comments (`<summary>`, `<param>`, `<returns>`, `<example>`)
    - [x] Handle `CancellationToken` parameter filtering
  - [x] `SignalROpenApiDocumentGenerator`: Converts hub metadata → `OpenApiDocument`
    - [x] Model hub methods as POST operations under `/hubs/{HubName}/{MethodName}`
    - [x] Generate JSON schemas for parameter types and return types
    - [x] Support `[JsonPolymorphic]`/`[JsonDerivedType]` → `discriminator`/`oneOf`
    - [x] Add `x-signalr` vendor extension with hub name, method name, stream flag, client events
    - [x] Add `securitySchemes` when `[Authorize]` is detected
    - [x] Apply Data Annotation constraints to schemas
  - [x] `SignalROpenApiOptions` configuration class
  - [x] `AddSignalROpenApi()` / `MapSignalROpenApi()` extension methods
  - [x] XML doc comments on all public types and members
- [x] **`SignalR.OpenApi.Tests` project** (MSTest):
  - [x] Hub discovery tests with various hub shapes
  - [x] OpenAPI document generation tests
  - [x] Test hubs: basic, typed (`Hub<T>`), streaming, authorized, hidden, tagged, obsolete, polymorphic
- [x] **`SignalR.OpenApi.Sample` project**:
  - [x] Sample hubs demonstrating all supported attributes
  - [x] Verify `/openapi/signalr-v1.json` serves valid spec
- [x] Verify `build.yml` passes CI

**Deliverables**: `SignalR.OpenApi` NuGet package. Valid OpenAPI JSON from SignalR hubs. MSTest coverage. README with getting started guide.

---

### Phase 2 – SwaggerUI Integration ✅

**Goal**: Display the SignalR OpenAPI spec in SwaggerUI with a custom JavaScript plugin that replaces HTTP calls with SignalR invocations.

- [x] **`SignalR.OpenApi.SwaggerUi` project** (net8.0 class library):
  - [x] Configure `.csproj` with NuGet package metadata
  - [x] Embed `@microsoft/signalr` JS bundle as embedded resource
  - [x] Depend on `Swashbuckle.AspNetCore.SwaggerUI` for UI hosting
  - [x] Custom JavaScript plugin (`signalr-openapi-plugin.js`):
    - [x] Detect `x-signalr` extension on operations
    - [x] Replace "Try it out" HTTP execution with SignalR `HubConnection.invoke()`
    - [x] Register plugin via `ConfigObject.Plugins` (not `InjectJavascript`) for proper initialization timing
    - [x] Call `setRequest()`/`setMutatedRequest()` before `setResponse()` for correct response rendering
    - [x] Read request body from `oas3Selectors.requestBodyValue()` for correct parameter extraction
    - [x] Connection management (connect/disconnect per hub)
    - [x] Display invocation result in response panel
  - [x] CSS styling matching SwaggerUI look and feel
  - [x] `AddSignalRSwaggerUi()` / `UseSignalRSwaggerUi()` extension methods
  - [x] Security UI (JWT, Windows Auth via SwaggerUI built-in support)
  - [x] XML doc comments on all public types and members
- [x] **Playwright-based E2E tests**:
  - [x] SwaggerUI loads and renders operations from SignalR spec
  - [x] "Try it out" + "Execute" triggers SignalR invocation (not HTTP 404)
  - [x] Response panel shows invocation result
  - [x] Plugin changes method labels to INVOKE/STREAM/EVENT
- [x] Update sample and README

**Deliverables**: `SignalR.OpenApi.SwaggerUi` NuGet package. SwaggerUI with interactive SignalR invocation.

---

### Phase 3 – Streaming & Client Events UI *(MVP Complete → v1.0)* ✅

**Goal**: Add UI panels for streaming visualization and server-to-client event monitoring. After this phase, the library is ready for **stable v1.0 release**.

- [x] **Streaming UI improvements**:
  - [x] Accumulate stream items into a growing array (history)
  - [x] Show stream state: `"streaming"` → `"completed"` → `"error: ..."`
  - [x] **Stop Stream** button to cancel active subscriptions
  - [x] Show "STREAM" method label for streaming operations
  - [x] Prevent re-execution while a stream is active
- [x] **Client events panel**:
  - [x] Custom `SignalREventLog` panel for `x-signalr.clientEvent: true` operations
  - [x] Auto-subscribe to all client events when connecting to a hub
  - [x] Real-time event log with timestamps and JSON payloads
  - [x] **Clear Log** and **Connect & Listen** buttons
- [x] **Connection management improvements**:
  - [x] Connection status indicator per hub (green/red/yellow)
  - [x] Automatic reconnection handling
- [x] Tests and README updated

**Deliverables**: Updated `SignalR.OpenApi.SwaggerUi` with streaming + events. **🎉 Stable v1.0 release.**

---

### Phase 4 – Examples Support (Custom Attributes) ✅

**Goal**: Support multiple named example requests/responses via custom attributes and a consolidated provider pattern.

- [x] Custom attributes (`AllowMultiple = true`):
  - [x] `[SignalROpenApiRequestExamplesAttribute(Type examplesProviderType)]`
  - [x] `[SignalROpenApiResponseExamplesAttribute(Type examplesProviderType)]`
- [x] Consolidated provider interface:
  - [x] `ISignalROpenApiExamplesProvider<T>` — returns one or many named examples
  - [x] `SignalROpenApiExample<T>` — example with Name, Summary, Description, Value
- [x] Generator reads example attributes and populates `examples` in `requestBody` and `responses`
- [x] XML doc `<example>` tag continues to work for simple cases
- [x] Update sample with multiple example providers
- [x] Tests and README updated

**Deliverables**: Updated `SignalR.OpenApi` NuGet package. Multiple named examples in SwaggerUI.

---

### Phase 5 – FluentValidation, Schema Refinements & Form Input ✅

**Goal**: Apply FluentValidation rules to OpenAPI schemas, refine request body schema generation, support form-urlencoded input, and add polymorphic sub-endpoints.

#### FluentValidation Integration

- [x] **`SignalR.OpenApi.FluentValidation` project** (net8.0 class library):
  - [x] `FluentValidationSchemaProcessor` implementing `ISignalROpenApiSchemaProcessor`
  - [x] Resolve `IValidator<T>` for hub method parameter types from DI
  - [x] Map FluentValidation rules → OpenAPI schema properties:
    - [x] `NotNull`/`NotEmpty` → `required`
    - [x] `Length` → `minLength`/`maxLength`
    - [x] `Matches` → `pattern`
    - [x] `GreaterThan`/`LessThan` → `minimum`/`maximum`/`exclusiveMinimum`/`exclusiveMaximum`
    - [x] `InclusiveBetween`/`ExclusiveBetween` → range
    - [x] `EmailAddress` → `pattern` (email regex)
  - [x] Support child validators (nested validator discovery via ChildValidatorAdaptor)
  - [x] `AddSignalRFluentValidation()` extension method
  - [x] XML doc comments on all public types and members
- [x] **`SignalR.OpenApi.FluentValidation.Tests`** (MSTest): 13 tests
- [x] Added `ISignalROpenApiSchemaProcessor` extensibility interface to core library

#### Schema Flattening & Response Codes

- [x] **Flatten single complex object parameter**: When there is exactly one parameter whose type is a complex object, directly return its schema instead of wrapping it in a named property. This ensures FluentValidation rules apply and the JSON body matches the object's properties directly.
- [x] **204 No Content**: Methods returning `void`/`Task` use `204 No Content` instead of `200 OK`.
- [x] **XML doc `<see cref>` tag handling**: Added `GetNodeText()` helper to properly render `<see cref>` and `<paramref>` tags in descriptions.
- [x] **`x-signalr` extension enhancements**: Added `parameterCount`, `flattenedBody`, `discriminatorProperty`, `discriminatorValue` fields.

#### Form-Urlencoded Input Mode

- [x] **Content type toggle**: Operations with all primitive or single flat object parameters get both `application/json` and `application/x-www-form-urlencoded` schemas.
- [x] **Form value unwrapping**: JS plugin detects SwaggerUI's form-urlencoded wrapper format (`{fieldName: {value: "val", errors: []}}`) and unwraps to extract raw values.
- [x] **Type coercion**: Form fields produce strings — plugin coerces numeric and boolean strings to their proper types.

##### Form-Urlencoded Availability Rules

| Scenario | Form Available | Example |
|---|---|---|
| All primitive params | ✅ | `SendMessage(string user, string message)` |
| Single flat object | ✅ | `SendDirectMessage(SendMessageRequest req)` |
| Polymorphic (main endpoint) | ❌ JSON-only | `SendNotification(Notification n)` — `oneOf` schema |
| Polymorphic sub-endpoint | ✅ | `SendNotification/text`, `SendNotification/alert` |
| Multi-object params | ❌ JSON-only | `ReplyToMessage(ChatMessage orig, ChatMessage reply)` |

#### Polymorphic Sub-Endpoints

- [x] **OData-style sub-endpoints**: Each derived type in a `[JsonPolymorphic]` parameter gets its own endpoint with a flat schema (e.g., `/hubs/Chat/SendNotification/text`, `/hubs/Chat/SendNotification/alert`).
- [x] **Discriminator metadata**: Sub-endpoint `x-signalr` extension includes `discriminatorProperty` and `discriminatorValue`.
- [x] **Discriminator injection**: JS plugin injects the discriminator value **first** in the JSON object (required by System.Text.Json for polymorphic deserialization).
- [x] **Schema design**: Discriminator field is `readOnly: true` with `default` and single-value `enum`, hidden from form inputs.

#### Sample App

- [x] `SendMessage` — primitive parameters (FluentValidation N/A)
- [x] `SendDirectMessage` — single flat object (FluentValidation applied)
- [x] `ReplyToMessage` — two object parameters (wrapped schema, JSON-only)
- [x] `SendToGroup` — three primitive parameters
- [x] `SendNotification` — polymorphic parameter with sub-endpoints
- [x] `Countdown` — streaming method
- [x] Multiple example providers: `SendMessageExamplesProvider`, `ReplyToMessageExamplesProvider`, `NotificationExamplesProvider`

#### Tests

- [x] 82 total tests (59 core + 10 Playwright + 13 FluentValidation)
- [x] Playwright tests excluded from CI via `[TestCategory("Playwright")]`

**Deliverables**: `SignalR.OpenApi.FluentValidation` NuGet package. Schema flattening, 204 responses, form-urlencoded input, polymorphic sub-endpoints. Updated sample and README.

---

## Future (community contributions welcome)

These items are deferred beyond the initial release phases.

### Source Generator
- **`SignalR.OpenApi.SourceGenerator` project** (netstandard2.0, Roslyn generator)
- `IIncrementalGenerator` analyzing `MapHub<T>()` calls at compile time
- Generate code that registers `SignalRHubInfo` instances with DI (avoids reflection cost)
- Handle typed hubs, streaming detection, all supported attributes at compile time
- Composite discoverer merging source-gen + reflection (source gen wins on conflicts)
- Separate NuGet package: `SignalR.OpenApi.SourceGenerator`

### Document Injection Mode
- `IOpenApiDocumentTransformer` that merges SignalR operations into an existing REST API OpenAPI document
- Configuration option: `options.MergeIntoExistingDocument = true`
- Handle tag conflicts, path conflicts, schema merging
- Show SignalR hubs alongside REST APIs in a single SwaggerUI

### Advanced UI Features
- Target selector UI (All / Group / User) for invocation
- Group name / User ID input fields
- Multiple concurrent stream support

### NSwag Integration
- `SignalR.OpenApi.NSwag` package for NSwag UI hosting (alternative to Swashbuckle)
- `IDocumentProcessor` / `IOperationProcessor` implementations

### Performance & Scalability
- Document caching / versioning
- Performance optimization and benchmarking

### Result Type Unwrapping & HTTP Status Mapping
- **Return type unwrapping**: Extensible `IReturnTypeUnwrapper` interface allowing users to unwrap wrapper types so the OpenAPI schema shows the inner type
- **HTTP status mapping**: Map result statuses to OpenAPI response codes (e.g., invalid → 400, not found → 404). Produces multiple `responses` entries per operation.
- **UI integration**: SwaggerUI plugin interprets result status from the SignalR response and displays it with appropriate HTTP status color-coding
- **Extensibility**: Users register custom `IReturnTypeUnwrapper` and `IResultStatusMapper` implementations via DI

---

## Design Principles

- **DRY** (Don't Repeat Yourself): Shared logic extracted into reusable services; no duplicated code across projects
- **SRP** (Single Responsibility Principle): Each class has one reason to change
- **Dependency Injection**: All services registered via `IServiceCollection` extensions; `IOptions<T>` for configuration
- **Interface Segregation**: `IHubDiscoverer`, `ISignalROpenApiDocumentGenerator`, `IOpenApiOperationTransformer`, `IOpenApiSchemaTransformer`
- **Open/Closed Principle**: Extensible via custom transformers registered in DI — users can handle their own custom attributes without modifying library code
- **Central package management**: `Directory.Packages.props`
- **StyleCop Analyzers + IDisposableAnalyzers**: Latest preview builds, enforced across all projects
- **Nullable reference types**: Enabled everywhere
- **XML documentation**: Required on all public APIs (enforced by StyleCop / compiler warnings)
- **Re-use existing libraries**: `FluentValidation`, `Swashbuckle`, `@microsoft/signalr`, `Microsoft.AspNetCore.OpenApi`
