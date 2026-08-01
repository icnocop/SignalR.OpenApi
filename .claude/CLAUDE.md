# SignalR.OpenApi

OpenAPI 3.1 generation and Swagger UI for ASP.NET Core SignalR hubs. Hub methods become
POST operations, client events become GET operations, and a Swagger UI plugin invokes them
over a real SignalR connection.

## Projects

Solution: `SignalR.OpenApi.slnx` (all projects target net8.0).

| Project | Purpose |
|---|---|
| `src/SignalR.OpenApi` | Core: reflection hub discovery, OpenAPI document generation, ASP.NET Core integration |
| `src/SignalR.OpenApi.SwaggerUi` | Swagger UI JS plugin + CSS + embedded `@microsoft/signalr` bundle |
| `src/SignalR.OpenApi.FluentValidation` | Maps FluentValidation rules onto schema constraints |
| `test/SignalR.OpenApi.Tests` | Unit tests for the core library (no Playwright dependency) |
| `test/SignalR.OpenApi.SwaggerUi.Tests` | Swagger UI integration tests + Playwright E2E |
| `test/SignalR.OpenApi.FluentValidation.Tests` | FluentValidation integration tests |
| `samples/SignalR.OpenApi.Sample` | Demo app exercising every feature |

## Commands

```shell
dotnet build SignalR.OpenApi.slnx
dotnet test SignalR.OpenApi.slnx --filter "TestCategory!=Playwright"   # what CI runs
dotnet test SignalR.OpenApi.slnx                                       # includes E2E
```

Playwright E2E needs browsers installed first:
`pwsh test/SignalR.OpenApi.Tests/bin/Debug/net8.0/playwright.ps1 install`.

Run one area: `dotnet test test/SignalR.OpenApi.Tests/SignalR.OpenApi.Tests.csproj --filter "FullyQualifiedName~Polymorphic"`.

## Build constraints that will bite you

`Directory.Build.props` sets `TreatWarningsAsErrors=true` and `GenerateDocumentationFile=true`,
with StyleCop.Analyzers and IDisposableAnalyzers. In practice:

- **Every public member needs an XML doc comment**, including test fixtures. A missing
  `<summary>`, `<param>`, or `<returns>` fails the build.
- **One top-level type per file** (SA1402), and the file name must match the type (SA1649).
  Colocate fixtures as sibling files, not as extra types in one file.
- Nullable reference types are on; `LangVersion` is `preview`.
- Package versions are centrally managed in `Directory.Packages.props` — never put a
  `Version` on a `PackageReference`.
- `.editorconfig` disables SA1101 (`this.` prefix), SA1200 (using placement), and SA1633
  (file header), but the MIT header line is still the convention in every file.

## Architecture

`ReflectionHubDiscoverer` scans assemblies for `Hub`/`Hub<TClient>` types → `SignalRHubInfo`
→ `SignalROpenApiDocumentGenerator` → `OpenApiDocument`.

- Paths: `/hubs/{HubName}/{MethodName}` (POST), `/hubs/{HubName}/events/{EventName}` (GET).
  The hub name is the type name minus a trailing `Hub`, or `[EndpointName]`.
- Every operation carries an `x-signalr` extension: `hub`, `method`, `stream`, `clientEvent`,
  `parameterCount`, `flattenedBody`, `hubPath`, and for sub-endpoints
  `discriminatorProperty`/`discriminatorValue`. Client events add `parameterNames` and
  `eventDiscriminators`.
- `ISignalROpenApiSchemaProcessor` is the post-generation extension point, but it is only
  invoked from `CreateObjectSchema` — not for polymorphic `oneOf` wrappers, primitives,
  arrays, or dictionaries.

### Request body shapes

| Parameters | Schema | Form-urlencoded |
|---|---|---|
| All primitives | Flat object, one property per parameter | yes |
| Single complex object | Flattened — the object's properties become the root | yes, if flat |
| Multiple, any complex | Wrapper with one named property per parameter | no |
| Polymorphic (main) | `oneOf` + discriminator | no |
| Polymorphic sub-endpoint | Flat derived-type schema | yes |

### Schema recursion

`CreateSchemaForType` dispatches polymorphic types to `CreatePolymorphicSchema` *before* the
`schemaRegistry` cycle guard, so polymorphic types need their own guards. Two exist:
`polymorphicTypesInProgress` (re-entry into a hierarchy still being built emits a `$ref`) and
an explicit `derived.DerivedType == type` branch for a base that lists itself as its own
derived type. Removing either reintroduces an uncatchable `StackOverflowException` that kills
the host process, not a failing assertion.

## Gotchas worth carrying into every session

- **System.Text.Json requires the discriminator property first** in the JSON object;
  anywhere else throws `InvalidDataException`. The JS plugin rebuilds objects with the
  discriminator first when injecting it.
- **SignalR's `JsonHubProtocol` is camelCase by default**, independent of the generator's
  `JsonSerializerOptions`. Wire names and OpenAPI names can differ — always match property
  names case-insensitively across that boundary.
- **SignalR omits polymorphic discriminators.** `WriteArguments` serializes with
  `argument.GetType()`, so the discriminator is never written for client events. That is why
  `eventDiscriminators` exists: the plugin infers the type by matching properties.
- **Register the Swagger UI plugin via `ConfigObject.Plugins`**, not `InjectJavascript` —
  the latter loads after initialization, so `wrapActions` hooks never apply.
- **`wrapComponents` props may be plain JS or ImmutableJS** depending on the component.
  Use defensive access: `val.get ? val.get(0) : val[0]`, `val.size != null ? val.size : val.length`.

## Test conventions

There is no shared fixture folder. Every test area owns the hubs and models it uses:

One test project per source project, each mirroring the folder layout of the code it
tests (`Discovery/`, `Generation/`, then the type name):

```
test/SignalR.OpenApi.Tests/
  Discovery/
    ReflectionHubDiscoverer/        discoverer tests + their hubs, Models/
  Generation/
    SignalROpenApiDocumentGenerator/
      GeneratorTestHelper.cs
      GenerateDocument/             one folder per method under test
        DocumentInvariantsTests.cs
        <Scenario>/                 one folder per scenario
          <Scenario>Tests.cs
          <Scenario>Hub.cs
          Models/                   data types used by that scenario only

test/SignalR.OpenApi.SwaggerUi.Tests/
  SwaggerUiIntegrationTests.cs      in-process host via TestHost
  SwaggerUi*PlaywrightTests.cs      browser E2E
  Hubs/, Models/                    this project's own test app
```

Namespaces follow the folders (`SignalR.OpenApi.Tests.Discovery`,
`SignalR.OpenApi.Tests.Generation.GenerateDocument.<Scenario>`) except that the type-name
folder is skipped — a namespace `...Generation.SignalROpenApiDocumentGenerator` would
collide with the type of that name. Note these test namespaces **shadow** the product
`SignalR.OpenApi.Discovery` / `.Generation` namespaces from inside `SignalR.OpenApi.Tests`,
so a partially qualified `Discovery.X` there resolves to the test namespace; fully qualify
as `OpenApi.Discovery.X` when that happens.

- Use `GeneratorTestHelper.GenerateFor(typeof(SomeHub))` so a test sees only its own hubs.
  It sets `SignalROpenApiOptions.HubFilter` under the hood. Without it, a test's document
  contains every hub in the assembly and becomes coupled to unrelated fixtures. The Swagger
  UI test hosts set `HubFilter` for the same reason.
- `GenerateForAllHubs()` is only for document-wide invariants (see `DocumentInvariantsTests`),
  and those calls are commented as deliberate.
- **Hub type names must be unique across the test assembly.** The hub name (type name minus
  a trailing `Hub`) becomes the OpenAPI path segment, so two hubs sharing a simple name in
  different namespaces would collide in `GenerateForAllHubs`. Prefix new fixtures with their
  scenario.
- Folder names are for navigation; namespaces do not mirror them. Files under a scenario's
  `Models/` keep the scenario's namespace, and no analyzer enforces otherwise.
- Test naming: `Method_Scenario_ExpectedResult`.
- Playwright tests live only in `SignalR.OpenApi.SwaggerUi.Tests`, are tagged
  `[TestCategory("Playwright")]`, and are excluded in CI. Install browsers with
  `pwsh test/SignalR.OpenApi.SwaggerUi.Tests/bin/Debug/net8.0/playwright.ps1 install`.
- SwaggerUI's responses section renders more than one `<select>` (a media-type dropdown as
  well as the named-examples dropdown), so locate the examples one by its option text
  rather than taking `.First`.

## Docs maintenance

- `README.md` is the user-facing documentation — update it whenever features, options, UI
  behavior, or public API change.
- Add a `CHANGELOG.md` entry under `## [Unreleased]`. Describe the actual defect or feature,
  not the commit subject.

## Tooling

`.claude/skills/` is a link to `.github/copilot/skills/`, so the same skills serve Copilot
and Claude Code. Run `.claude/link-gitHub.ps1` to create or repair it; the link itself is
gitignored.
