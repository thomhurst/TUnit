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

Source-generated and reflection metadata feed the same execution path. Roslyn compatibility projects (`*.Roslyn414`, `*.Roslyn44`, `*.Roslyn47`) link shared sources from their base project; account for those variants when changing generators or analyzers.
