---
name: signalr-openapi
description: Developing SignalR.OpenApi — an ASP.NET Core library generating OpenAPI specs and SwaggerUI for SignalR hubs. Use when modifying hub discovery, OpenAPI document generation, SwaggerUI plugin JS, FluentValidation schema processing, or the sample app. Covers System.Text.Json polymorphic serialization gotchas, Swashbuckle plugin registration, form-urlencoded handling, and embedded resource serving.
---

# SignalR.OpenApi Development

## Solution Structure

| Project | Purpose |
|---------|---------|
| `src/SignalR.OpenApi` | Core: hub discovery (reflection), OpenAPI 3.1 generation, ASP.NET Core integration |
| `src/SignalR.OpenApi.SwaggerUi` | SwaggerUI JS plugin + CSS + embedded `@microsoft/signalr` bundle |
| `src/SignalR.OpenApi.FluentValidation` | FluentValidation rules → OpenAPI schema properties |
| `test/SignalR.OpenApi.Tests` | MSTest unit + Playwright E2E tests |
| `test/SignalR.OpenApi.FluentValidation.Tests` | FluentValidation integration tests |
| `samples/SignalR.OpenApi.Sample` | Demo app with all feature scenarios |

## Build & Test

```shell
dotnet build SignalR.OpenApi.slnx
dotnet test SignalR.OpenApi.slnx                              # all tests (needs Playwright browsers)
dotnet test SignalR.OpenApi.slnx --filter "TestCategory!=Playwright"  # CI-safe
```

## Critical Implementation Details

### System.Text.Json Polymorphic Deserialization

The type discriminator property **must appear first** in the JSON object. Any other position causes `InvalidDataException`.

```
✅ {"type":"text","content":"Hello","recipient":"Alice"}
❌ {"content":"Hello","recipient":"Alice","type":"text"}
```

When injecting discriminator values in JS, always create a new object with discriminator first:
```js
var reordered = {};
reordered[discriminatorProp] = discriminatorValue;
Object.keys(original).forEach(k => { if (k !== discriminatorProp) reordered[k] = original[k]; });
```

### SignalR vs OpenAPI JSON Serialization — Naming Policy Mismatch

SignalR's `JsonHubProtocol` and the OpenAPI document generator use **separate** `JsonSerializerOptions` instances with potentially different `PropertyNamingPolicy` settings:

- **SignalR wire format**: `JsonHubProtocol.PayloadSerializerOptions` defaults to **camelCase** (e.g., `content`, `recipient`)
- **OpenAPI spec**: Configurable via `SignalROpenApiOptions.JsonSerializerOptions` (e.g., `PropertyNamingPolicy = null` → PascalCase: `Content`, `Recipient`)

**Rule**: Always use **case-insensitive matching** when comparing property names across SignalR wire data and OpenAPI spec metadata. Build a lowercase lookup of received keys:
```js
var keysLower = {};
Object.keys(obj).forEach(function (k) { keysLower[k.toLowerCase()] = true; });
if (keysLower[candidateProp.toLowerCase()]) { /* match */ }
```

### SignalR Omits Polymorphic Type Discriminators

`JsonHubProtocol.WriteArguments` serializes each argument via `JsonSerializer.Serialize(writer, argument, argument.GetType(), options)` — using the **runtime type**. `System.Text.Json` only writes the discriminator when serializing as the **declared base type** (e.g., `Notification`), not the runtime derived type (e.g., `TextNotification`).

**Consequence**: Client event payloads for polymorphic types are MISSING the discriminator property. The `x-signalr.eventDiscriminators` metadata provides property-to-type mappings so the JS plugin can infer and inject the discriminator by matching received properties against known derived types.

### Swashbuckle Plugin Registration

Register via `ConfigObject.Plugins`, **not** `InjectJavascript`. `InjectJavascript` loads scripts after SwaggerUI initialization, so `wrapActions` hooks are never applied.

```csharp
options.ConfigObject.Plugins = new[] { "SignalROpenApiPlugin" };
```

### SwaggerUI Component Wrapping (`wrapComponents`)

Props passed to wrapped components may be **plain JS objects/arrays OR ImmutableJS** structures (Maps, Lists) depending on the component. Never assume one or the other — always use defensive access:

```js
// Reading from a prop that may be array or ImmutableJS List
var path = props.pathMethod.get ? props.pathMethod.get(0) : props.pathMethod[0];

// Checking collection size (ImmutableJS uses .size, arrays use .length)
var count = params.size != null ? params.size : params.length;
```

Key components wrapped by the plugin:
- `OperationSummary` — changes method labels (INVOKE/STREAM/EVENT), strips Async suffix from display path
- `curl` — hidden for SignalR operations (returns `null`)
- `parameters` — hidden when empty for SignalR operations (suppresses "No parameters" message since hub methods use request body, not URL parameters)
- `execute` — shows "Stop Stream" button for active streams
- `responses` — renders `SignalREventLog` panel for client events (GET operations)

### SwaggerUI Form-Urlencoded Behavior

- SwaggerUI wraps form values as `{fieldName: {value: "val", errors: []}}` — must unwrap
- `readOnly: true` fields are hidden from both form inputs AND JSON textarea
- `oneOf` schemas always fall back to JSON textarea (cannot render as forms)
- Only flat `type: "object"` schemas with primitive properties render as form fields

### Request Body Schema Rules

| Scenario | Schema Shape | Form Available |
|----------|-------------|----------------|
| All primitive params | Flat object with each param as property | ✅ |
| Single complex object param | Flattened — object's properties become root | ✅ (if flat) |
| Multiple complex params | Named wrapper properties | ❌ JSON only |
| Polymorphic param (main) | `oneOf` with discriminator | ❌ JSON only |
| Polymorphic sub-endpoint | Flat derived-type schema | ✅ |

### OpenAPI Document Response Codes

- Methods returning `void`/`Task`/`ValueTask` → `204 No Content`
- Methods returning `Task<T>` → `200 OK` with schema
- Streaming (`IAsyncEnumerable<T>`, `ChannelReader<T>`) → `200 OK` with item schema

### x-signalr Vendor Extension

Every SignalR operation includes `x-signalr` with: `hub`, `method`, `stream`, `clientEvent`, `parameterCount`, `flattenedBody`, `hubPath`, and optionally `discriminatorProperty`/`discriminatorValue` for sub-endpoints.

Client event operations additionally include:
- `parameterNames` — maps positional SignalR args to named properties in the event log
- `eventDiscriminators` — property-to-type mapping for polymorphic parameter inference (compensates for SignalR omitting discriminators); uses case-insensitive matching for cross-boundary compatibility

### Playwright Tests in CI

Tests tagged `[TestCategory("Playwright")]` are excluded in CI via `--filter "TestCategory!=Playwright"` because GitHub Actions runners lack browser binaries.

## Key Files

- **`src/SignalR.OpenApi/Generation/SignalROpenApiDocumentGenerator.cs`** — Core generator: paths, operations, schemas, polymorphic sub-endpoints, form-urlencoded availability
- **`src/SignalR.OpenApi.SwaggerUi/Resources/signalr-openapi-plugin.js`** — JS plugin: intercepts HTTP execution, manages SignalR connections, form unwrapping, discriminator injection
- **`src/SignalR.OpenApi/Discovery/ReflectionHubDiscoverer.cs`** — Scans assemblies for Hub types
- **`src/SignalR.OpenApi.FluentValidation/FluentValidationSchemaProcessor.cs`** — Maps validator rules to OpenAPI schema constraints
