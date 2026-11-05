# Architecture and Source Locations

Product projects live in `src/`, tests in `tests/`, and benchmarks in `benchmarks/`. Test project names generally match the product project. Examples are in `examples/`; build imports in `eng/`; developer scripts in `scripts/`; CI orchestration in `tools/TUnit.Pipeline`. Tag a new pipeline test module whose result cannot depend on the operating system with `[ModuleCategory(PipelineCategories.PlatformNeutral)]`: macOS skips those suites on pull requests because Ubuntu and Windows already run them.

| Project under `src/` | Responsibility |
| --- | --- |
| `TUnit.Core` | Public abstractions, attributes, and interfaces |
| `TUnit.Core.SourceGenerator` | Compile-time test metadata collection |
| `TUnit.Engine` | Reflection metadata collection and shared execution |
| `TUnit.Assertions` / `TUnit.Assertions.SourceGenerator` | Fluent assertions and `[GenerateAssertion]` |
| `TUnit.Analyzers` / `TUnit.Analyzers.CodeFixers` | Test diagnostics and fixes; other libraries have their own analyzer projects |
| `TUnit.Mocks` / `TUnit.Mocks.SourceGenerator` | Mock runtime and generated implementations; sibling projects provide integrations |
| `TUnit.Templates` | Packaged `dotnet new` templates |
| `TUnit.Analyzers.Internal` | Repo-internal analyzer (never shipped): `TUNITINT001` rejects culture-sensitive number/date formatting and parsing in build-time projects |

Source-generated and reflection metadata feed the same execution path. Roslyn compatibility projects (`*.Roslyn414`, `*.Roslyn44`, `*.Roslyn47`) link shared sources from their base project; account for those variants when changing generators or analyzers.

Generators, analyzers, code fixers and build tasks run inside consumers' compilers, so their behaviour must not depend on the build machine's culture (sv-SE renders negative numbers with U+2212, de-DE uses decimal commas, tr-TR lowercases `I` to `ı`). `Directory.Build.targets` applies `TUNITINT001` and `eng/BuildTimeGlobalization.globalconfig` (CA1304/CA1305/CA1309/CA1310/CA1311 as errors) to those projects; format and parse with `CultureInfo.InvariantCulture` and compare strings with `StringComparison.Ordinal`. `TUNITINT001` cannot see a number that is already typed `object` or an unconstrained generic before it is formatted (other than Roslyn's `TypedConstant.Value`); convert such values with `ToInvariantString()` or `Convert.ToString(value, CultureInfo.InvariantCulture)`. For a justified exception, such as text that is meant to follow the user's culture, wrap the line in `#pragma warning disable TUNITINT001` / `restore` with a comment explaining why; a false positive caused by a Roslyn or SDK change can be downgraded with `dotnet_diagnostic.TUNITINT001.severity` in `eng/BuildTimeGlobalization.globalconfig`.

## Whole-tuple method inference

Generated and reflection discovery must preserve the same whole-row boundary before inferring method types. `WholeTupleInferenceTests` snapshots the shared `_7638/WholeTupleInferenceTests.cs` fixtures; `Issue7638Tests` checks counts and failure outcomes in reflection and Native AOT modes in CI.

| Rule | Generator | Runtime |
| --- | --- | --- |
| A tuple first argument with omitted optional, params, or collecting-array tails requires invariant constructed structure and consistent repeated generic bindings | Whole-row matcher in `TestMetadataGenerator` | `TupleHelper.MatchesOpenTuple`, used by `DataSourceHelpers`, `TestDataRowUnwrapper`, and `DataUnwrapper` |
| A nested row that does not match the whole first tuple spreads into separate columns | Row-type inference falls back to ordinary column inference | Type-aware unwrapping spreads the row |
| Explicit method types supplement inferred whole-row instantiations, deduplicating identical types | `InferTypesFromMethodDataSourceRowType` | `ReflectionTestDataCollector.InferTypesFromMethodDataSource` |
| Async source declarations and outer Func, derived TestDataRow, and inner Func wrappers normalize before inference | `GetMethodDataSourceRowType` | `MethodDataSourceAttribute.GetDeclaredRowType` and `TestDataRowUnwrapper.GetInnerDataType` |
| Ordinary spread columns retain last-column inference, including arrays and nullable values | `ProcessTypeForGenerics` | `MapGenericParameters` |

Sole-tuple element conversion and reconstruction retain their existing behavior. Analyzer false positives for these whole-row shapes are tracked separately in #7622; matcher maintainability is tracked in #7588.
## Sole tuple method-type inference

The generator uses Roslyn symbols and cannot share the runtime matcher. Keep these policies aligned. `Issue7639Tests` runs the shared `_7639/SoleTupleInferenceTests.cs` fixtures in reflection and Native AOT (source-generated) modes in CI. `SoleTupleInferenceTests` snapshots their generated metadata and invocation.

| Rule | Generator | Runtime |
| --- | --- | --- |
| A sole tuple argument is reconstructed; closed elements may convert at invocation | `MatchesOpenRowType` reconstruction path | `TestGenericTypeResolver.TryInferTypeMapping` tuple-element loop |
| Structural open elements (direct, nullable, array, matching generic) use the last repeated binding | Reconstruction with overwrite bindings | Reconstruction with overwrite bindings |
| Elements without a structural match defer casts to invocation and supply no bindings | Reconstruction fallback | Reconstruction fallback |
| Interface/base candidates isolate bindings, retain strict repeated bindings through nested tuples, and validate completed types and constraints | Candidate copies and completed-type conversion check | Candidate copies and completed-type assignability |
| Ordinary spread columns retain ordinary inference | `ProcessTypeForGenerics` | Ordinary `TryInferTypeMapping` and `ReflectionTestDataCollector.InferGenericTypeMapping` |

Unmatched open elements, including object, interface, base and array declarations, require another element to infer method types before invocation can cast them. Closed reconstructed elements may fail at invocation if their values cannot convert; inference does not reject them from their declared types alone. Tuple payload boundaries, including nested singleton tuples and `TestDataRow` payloads, must survive unwrapping until reconstruction.
