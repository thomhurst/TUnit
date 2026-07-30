# TUnit.Mocks internals access (experimental)

Prototype for [#6514](https://github.com/thomhurst/TUnit/issues/6514) "Tier 2": make types that
are `internal` to another assembly first-class mockable — nameable in test code, source-generated
typed mocks, setups, matchers, and verification — even when that assembly grants **no**
`InternalsVisibleTo` at all.

This complements the runtime auto-stubs from PR #6519 (Tier 1). Stubs are zero-config but
anonymous: they satisfy an SDK's internal `Get<T>()` calls with functional defaults, yet the test
can never configure or verify them, because it cannot write the type's name. Internals access
removes that constraint entirely.

## Usage

```xml
<PropertyGroup>
  <TUnitMocksExperimentalInternalsAccess>true</TUnitMocksExperimentalInternalsAccess>
</PropertyGroup>

<ItemGroup>
  <!-- Simple assembly name of any direct or transitive reference. -->
  <TUnitMocksInternalsAccess Include="Microsoft.Azure.Functions.Worker.Core" />
</ItemGroup>

<Import Project="path/to/TUnit.Mocks.InternalsAccess.targets" />
```

Then internal types of that assembly are usable in test code like public ones:

```csharp
var bindings = IFunctionBindingsFeature.Mock();          // internal to the SDK — now mockable
features.Get<IFunctionBindingsFeature>().Returns(bindings.Object);
features.Get<IFunctionBindingsFeature>().WasCalled(Times.Once);
```

## How it works

The established "publicizer" pattern (Krafs.Publicizer, IgnoresAccessChecksToGenerator), wired
for TUnit.Mocks:

1. After reference resolution, the `PublicizeAssemblyReferences` task rewrites each requested
   reference (Mono.Cecil) so its internal types and members are public, preserving the assembly
   identity (name, version, public key). The copy lives under `obj/` only.
2. The swap happens on `ReferencePathWithRefAssemblies` — the item group that feeds the compiler
   and nothing else. `ReferencePath` is untouched, so copy-local output and `deps.json` keep the
   original assembly and the runtime loads the real thing.
3. The task emits `[assembly: IgnoresAccessChecksTo("...")]` (plus the attribute definition) into
   the compilation. CoreCLR honors it and skips accessibility checks from the test assembly to
   the named assemblies, so the compiled IL — including generated mock classes implementing
   internal interfaces — loads and runs against the original assembly.
4. The TUnit.Mocks source generator needs no changes: through the publicized reference the
   internal types simply look public, so discovery, TM007 accessibility checks, and mock
   emission all behave as for any public type.

## Caveats (why this is experimental)

- `IgnoresAccessChecksToAttribute` is honored by CoreCLR but is not a documented public contract.
  It is the foundation of several long-lived OSS packages, so breakage risk is low but nonzero.
- .NET Framework does not honor the attribute — the pipeline is inert there (warning emitted).
- Native AOT / trimming behavior is unverified.
- The publicizer currently rewrites type and method accessibility (fields are left alone) and
  skips explicit interface implementations.
- MSBuild loads the task assembly in-proc and holds a file lock across builds in the same node;
  after editing the task itself, run `dotnet build-server shutdown` before rebuilding it.
- Not packaged yet: consumers import the `.targets` file directly and the task assembly is
  resolved from this project's build output. Packaging (`tasks/` folder + `buildTransitive`)
  is the productionization step.

## Validation

`tests/TUnit.Mocks.InternalsAccess.TargetLib` stands in for a third-party SDK: an internal
interface, a public generic accessor requested from SDK-internal code, and deliberately **zero**
IVT grants — the exact case Tier 1 cannot reach. `tests/TUnit.Mocks.InternalsAccess.Tests`
exercises naming, mocking, typed setup/matcher/verification, and a hand-written implementation
of the internal interface (type-load proof).
