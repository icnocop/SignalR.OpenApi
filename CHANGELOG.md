# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versions tagged `-ci.*` are prereleases published from CI and are not listed separately;
their changes appear under the next stable release.

## [1.1.68] - 2026-08-01

### Fixed

- Schema generation no longer overflows the stack when a `[JsonPolymorphic]` type lists
  itself as one of its own `[JsonDerivedType]` entries. This is valid System.Text.Json —
  it is required when instances of the base type are serialized as-is — but the generator
  routed the self entry back through the polymorphic branch indefinitely, crashing the
  host process whenever the document was requested. The base type is now emitted as one of
  its own `oneOf` variants with the correct discriminator mapping. A general guard also
  covers polymorphic hierarchies that cycle back on themselves through a property.
- SignalR connections no longer fail in browser hosts that expose a Node-like `process`
  global. The bundled `@microsoft/signalr` client treated any such host as Node.js and its
  `HttpConnection` constructor unconditionally `require()`d Node-only modules (`eventsource`,
  `ws`) that do not exist in a browser, so the connection could not be constructed. The
  bundled `signalr.min.js` now forces `isNode` to `false` and the browser-native WebSocket,
  EventSource, and fetch APIs are used. Electron.NET's renderer was one affected host.

## [1.0.429] - 2026-04-29

### Added

- Response examples can be supplied by providers resolved from dependency injection,
  including scoped services.

### Changed

- The Swagger UI connection bar is rendered once per hub rather than once per tag, and its
  position now follows the document-level tag list so it appears above the first visible
  tag section.
- The hub's primary tag honors the Swagger UI `tagsSorter` setting: alphabetical order when
  `tagsSorter` is `"alpha"`, otherwise document tag order.
- Hubs whose methods all use custom `[Tags]` are now mapped correctly by the plugin, so the
  connection bar still appears when no operation tag matches the hub name.

### Fixed

- Hub connection setup is wrapped in error handling and connection failures are reported
  inline in the UI instead of failing silently.
- Hub URLs are resolved to absolute URLs before connecting.

## [1.0.320] - 2026-03-19

### Added

- Configurable `SecuritySchemes` on `SignalROpenApiOptions`, replacing the hardcoded Bearer
  scheme, so any authentication scheme can be declared and referenced by operations.
- `[Tags]` on client event methods, overriding the default `"{HubName} Events"` grouping.
- A per-hub connection bar in Swagger UI with a connect/disconnect toggle, auto-connect on
  execute, and automatic reconnect when credentials change.
- `DocExpansion`, `SortTagsAlphabetically`, and `SortOperationsAlphabetically` options.
- A hub method taking both an object and a primitive parameter is now covered by schema
  generation tests, confirming it produces a JSON-only wrapper schema.

### Changed

- The connection UI was redesigned around a single toggle button styled to match Swagger
  UI's Execute button, replacing the separate status label and buttons.
- When every client event on a hub has a custom `[Tags]` attribute, the default
  `"{HubName} Events"` tag is no longer added.

### Fixed

- apiKey credentials are no longer sent as Bearer tokens; the plugin now distinguishes
  apiKey from Bearer and Basic schemes, and supports several schemes at once.
- Long Polling is forced when API key headers are configured, because browsers cannot send
  custom headers over WebSocket or Server-Sent Events.
- Server errors returned from hub invocations are formatted with actionable hints instead
  of being surfaced raw.

## [1.0.319] - 2026-03-18

### Added

- Document-level `tags` are collected from hub methods and client events, with descriptions
  from `SignalROpenApiOptions.TagDescriptions` or, when the tag matches the hub name, the
  hub's XML `<summary>`.
- Static per-connection headers (`SignalRSwaggerUiOptions.Headers`) and user-entered headers
  (`SignalROpenApiOptions.ApiKeyHeaders`), the latter emitted as apiKey security schemes and
  surfaced in the Swagger UI Authorize dialog.
- `SyntaxHighlight` and `DefaultModelsExpandDepth` options.

### Changed

- Enum schemas follow the configured `JsonSerializerOptions`: enums serialized by
  `JsonStringEnumConverter` produce string schemas with member names, otherwise integer
  schemas with numeric values. This applies to standalone enums and enum properties alike.
- Syntax highlighting and the models section are off by default, and the plugin caches its
  converted view of the spec and reads it via ImmutableJS `getIn()`, which noticeably
  improves Swagger UI responsiveness on large documents.

## [1.0.318] - 2026-03-17

### Added

- Example providers support constructor injection, resolved through `ActivatorUtilities`.
- Options for property naming policy and for hiding the type discriminator from JSON examples.
- Property-level examples on form-urlencoded request schemas, filtered by derived type for
  polymorphic endpoints and skipped for read-only properties.
- `parameterNames` and `eventDiscriminators` in the `x-signalr` extension for client events.
  SignalR serializes arguments using their runtime type, so polymorphic payloads arrive
  without a discriminator; this metadata lets the plugin infer the type by matching
  properties, and it matches case-insensitively because the SignalR wire format and the
  OpenAPI document can use different naming policies.

### Changed

- XML documentation is resolved through `<inheritdoc />`, so hub methods inherit summaries,
  remarks, parameter docs, and return docs from the interfaces they implement.
- The `Async` suffix is stripped only for Swagger UI display; operation IDs and method names
  keep it.
- Polymorphic sub-endpoints include only the request examples whose value matches that
  sub-endpoint's derived type.

## [1.0.226] - 2026-02-25

### Added

- Integer type discriminators for polymorphic schemas, in addition to strings. The
  discriminator property's type, enum, and default follow the discriminator's type.

### Changed

- The discriminator property name defaults to `"$type"` when not specified, and the resolved
  property name is used consistently in both the schema and the mapping.
- Already-generated derived type schemas are reused from the registry rather than rebuilt.

### Fixed

- Self-referencing types no longer overflow the stack during schema generation. Processed
  types are tracked and later occurrences emit a schema reference.

## [1.0.223] - 2026-02-22

### Changed

- Package versions use a single combined UTC timestamp (`yyyy-MM-dd'T'HH-mm-ss'Z'`) instead
  of separate date and time stamps.

## [1.0.222.13] - 2026-02-22

First release.

### Added

- OpenAPI 3.1 document generation for ASP.NET Core SignalR hubs, with reflection-based hub
  discovery. Hub methods are modeled as POST operations under `/hubs/{HubName}/{MethodName}`
  and client events as GET operations under `/hubs/{HubName}/events/{EventName}`.
- An `x-signalr` vendor extension carrying the SignalR-specific metadata the UI needs.
- Polymorphic types generate `oneOf` schemas with a discriminator, plus one flat,
  form-friendly sub-endpoint per derived type.
- A Swagger UI plugin that invokes hub methods over a real SignalR connection and logs
  client events.
- FluentValidation integration mapping validator rules onto schema constraints.
- Support for `[Authorize]`, `[Tags]`, `[EndpointName]`, `[ApiExplorerSettings]`,
  `[Obsolete]`, and data annotations.
- Request and response examples via `ISignalROpenApiExamplesProvider<T>`.
- A `publishRelease` workflow input for publishing stable, non-prerelease packages.

[1.1.68]: https://github.com/icnocop/SignalR.OpenApi/compare/v1.0.429...v1.1.68
[1.0.429]: https://github.com/icnocop/SignalR.OpenApi/compare/v1.0.320...v1.0.429
[1.0.320]: https://github.com/icnocop/SignalR.OpenApi/compare/v1.0.319...v1.0.320
[1.0.319]: https://github.com/icnocop/SignalR.OpenApi/compare/v1.0.318...v1.0.319
[1.0.318]: https://github.com/icnocop/SignalR.OpenApi/compare/v1.0.226...v1.0.318
[1.0.226]: https://github.com/icnocop/SignalR.OpenApi/compare/v1.0.223...v1.0.226
[1.0.223]: https://github.com/icnocop/SignalR.OpenApi/compare/v1.0.222.13...v1.0.223
[1.0.222.13]: https://github.com/icnocop/SignalR.OpenApi/releases/tag/v1.0.222.13
