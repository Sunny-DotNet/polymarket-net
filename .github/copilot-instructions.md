# Copilot Instructions

## Build, test, and lint commands

- Restore the solution:
  - `dotnet restore Polymarket.Client.slnx`
- Build the solution:
  - `dotnet build Polymarket.Client.slnx --configuration Release --no-restore`
- Run the full test project:
  - `dotnet test tests\Polymarket.Client.Tests\Polymarket.Client.Tests.csproj --configuration Release`
- Run a single test:
  - `dotnet test tests\Polymarket.Client.Tests\Polymarket.Client.Tests.csproj --filter "FullyQualifiedName~Polymarket.Client.Tests.ClobClientTests.GetVersionAsync_ReturnsVersionFromPayload"`
- CI and publishing are intentionally modeled after `Sunny-DotNet/open-hub-agent`:
  - CI runs restore/build/test on `windows-latest`
  - NuGet publishing uses tag-triggered `publish-nuget.yml`
  - package version is derived from the git tag inside the workflow
  - pack runs with `ContinuousIntegrationBuild=true`
  - release publishes `.nupkg` artifacts through `NuGet/login@v1` Trusted Publishing and also pushes them to GitHub Packages

## High-level architecture

- This repository is intended to become a .NET SDK for **Polymarket CLOB v2**.
- The package and root namespace should be **`Polymarket.Client`**.
- Repository layout should follow the same high-level shape as `Sunny-DotNet/CommonCrawl.Net`:
  - root solution file plus shared `Directory.Build.props` / `Directory.Packages.props`
  - `src/Polymarket.Client` for the SDK
  - `tests/Polymarket.Client.Tests` for tests
  - `examples/Polymarket.Client.ConsoleApp` for usage samples
- The current first version already establishes the public `ClobClient` facade plus repository-wide build/package conventions.
- `ClobClient` currently covers the smallest public slice of the Polymarket API:
  - `/ok`
  - `/version`
  - `/time`
  - chain-specific contract address resolution for `Polygon` and `Amoy`
- Keep the split between public facade and internal protocol details:
  - public types such as `ClobClient`, `ClobClientOptions`, `Chain`, and `ContractConfig` stay in the main namespace
  - endpoint strings, response DTOs, and contract registry details live under `src/Polymarket.Client/Internal`
- Expected capability areas:
  - health/version/time endpoints
  - market discovery, market details, order books, prices, spreads, and price history
  - API key lifecycle and layered authentication
  - order construction, signing, posting, cancellation, and scoring
  - account/trade/notification/reward/builder endpoints
- Authentication is expected to stay explicitly layered:
  - **L0**: anonymous market-data access
  - **L1**: wallet signature for creating/deriving API keys
  - **L2**: HMAC-signed account and trading requests
- Keep `ClobClient` as the public facade, but separate HTTP transport, signing/auth header generation, serialization, and order-building logic into internal components rather than collapsing everything into one class.

## Key conventions

- Align behavior and general usage with the official Polymarket TypeScript and Python SDKs first; use them as the main compatibility baseline when behavior is unclear.
- Convert the public API to .NET naming:
  - PascalCase for types and members
  - async methods should use the `Async` suffix, e.g. `GetMarketsAsync`
- Keep the SDK **async-first**. If synchronous wrappers are ever added, they should be secondary to the async API rather than the primary surface.
- Prefer strongly typed C# models (`record`, `class`, `enum`) over dictionary-like payloads exposed to callers.
- Preserve protocol naming in JSON/contracts, but map it to .NET-friendly model/property names in code.
- Thread `CancellationToken` through asynchronous public APIs.
- Prefer injectable `HttpClient` and testable abstractions for time, signing, and HTTP behavior.
- Tests currently stub HTTP through a custom `HttpMessageHandler`; keep client code easy to exercise that way rather than baking transport logic into static helpers.
- Avoid silent fallbacks in the .NET implementation; surface explicit exceptions or typed error results consistent with the eventual SDK design.
- Cache market metadata that the reference SDKs cache as part of the client flow, especially tick size, neg-risk flags, and fee-related metadata.
- Treat `Polymarket.Client` as the package identity unless the repository later adds an explicit replacement.
- Follow the `open-hub-agent` packaging style when the .NET projects are created:
  - put repository-wide package metadata in `Directory.Build.props`
  - keep shared package versions in `Directory.Packages.props` instead of repeating versions in each `.csproj`
  - include `RepositoryUrl`, `RepositoryType`, `PublishRepositoryUrl`, `EmbedUntrackedSources`, and `DebugType=embedded`
  - pack the root `README.md` into NuGet packages via `PackageReadmeFile`
