# Architecture and Source Locations

Product projects live in `src/`, tests in `tests/`, and benchmarks in `benchmarks/`. Test project names generally match the product project. Examples are in `examples/`; build imports in `eng/`; developer scripts in `scripts/`; CI orchestration in `tools/TUnit.Pipeline`.

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
