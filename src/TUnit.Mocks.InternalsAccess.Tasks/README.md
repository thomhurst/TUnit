# TUnit.Mocks internals access (experimental)

The Tier 2 answer to [#6514](https://github.com/thomhurst/TUnit/issues/6514): make types that are
`internal` to another assembly first-class mockable — nameable in test code, source-generated
typed mocks, setups, matchers, and verification — even when that assembly grants **no**
`InternalsVisibleTo` at all. Complements the runtime auto-stubs from PR #6519 (Tier 1), which are
zero-config but anonymous: a stub satisfies an SDK's internal `Get<T>()` calls, yet the test can
never configure or verify a type it cannot name.

## Usage

Ships inside the `TUnit.Mocks` package; consumers only need the opt-in:

```xml
<PropertyGroup>
  <TUnitMocksExperimentalInternalsAccess>true</TUnitMocksExperimentalInternalsAccess>
</PropertyGroup>

<ItemGroup>
  <!-- Simple assembly name of any direct or transitive reference. -->
  <TUnitMocksInternalsAccess Include="Microsoft.Azure.Functions.Worker.Core" />
</ItemGroup>
```

## How it works

The established "publicizer" pattern (Krafs.Publicizer, IgnoresAccessChecksToGenerator), wired
for TUnit.Mocks:

1. `PublicizeAssemblyReferences` (Mono.Cecil) rewrites each requested reference so its internal
   types and members are public, preserving the assembly identity (name, version, public key).
   The **implementation** assembly is used as the source — Roslyn reference assemblies strip
   internal members (e.g. internal constructors) when no `InternalsVisibleTo` exists, so
   publicizing a ref assembly would yield empty shells. Copies live under `obj/` only.
2. `TUnit.Mocks.InternalsAccess.targets` (imported by `TUnit.Mocks.targets`, fully inert without
   the opt-in) swaps the copies into `ReferencePathWithRefAssemblies` — the item group that feeds
   the compiler and nothing else. `ReferencePath` is untouched, so copy-local output and
   `deps.json` keep the original assembly and the runtime binds to it.
3. The task emits `[assembly: IgnoresAccessChecksTo(...)]` (plus the attribute definition) into
   the compilation; the runtime honors it and skips accessibility checks, so generated mock
   classes implementing internal interfaces load and run against the original assembly.
4. The TUnit.Mocks source generator needs no changes: through the publicized reference the
   internal types simply look public — discovery, TM007 accessibility checks, and emission all
   behave as for any public type.

## Layout

- Task assembly: `tasks/net472/` (Visual Studio's .NET Framework MSBuild) and `tasks/net8.0/`
  (`dotnet msbuild`), each with `Mono.Cecil.dll` beside it; selected via `$(MSBuildRuntimeType)`.
- Targets: `buildTransitive/<tfm>/TUnit.Mocks.InternalsAccess.targets`, imported from
  `TUnit.Mocks.targets`. Repo-local builds resolve the task from this project's bin instead.

## Caveats

- `IgnoresAccessChecksToAttribute` is honored by CoreCLR but is not a documented public contract
  (it underpins several long-lived OSS packages; breakage risk is low but nonzero).
- .NET Framework test targets are unsupported (warning `TUMIA002`, pipeline stays inert).
- Verified under `PublishTrimmed` (full trim mode); Native AOT not yet verified.
- Publicizer scope: types and methods (constructors and accessors included); fields are left
  alone; explicit interface implementations stay private as IL requires.
- Dev loop: MSBuild nodes hold the task assembly's file lock across builds — run
  `dotnet build-server shutdown` after changing this project.

## Validation

- `tests/TUnit.Mocks.InternalsAccess.TargetLib` — a strong-named stand-in SDK with an internal
  interface, internal generic interface, internal class (internal constructor), and public
  generic accessors called from SDK-internal code; deliberately **zero** IVT grants.
- `tests/TUnit.Mocks.InternalsAccess.Tests` — end-to-end pipeline tests (naming, mocking, typed
  setup/matcher/verification, partial-mocking the internal class, manual implementations) plus
  unit tests of the task itself (publicizing, identity preservation, incrementality, generated
  source, `TUMIA001` errors). Runs in CI via `RunMockInternalsAccessTestsModule`.
- Packaged-layout and trimmed-publish verification were exercised against the produced nupkg
  (extracted `buildTransitive` import + `PublishTrimmed=true` console consumer).
