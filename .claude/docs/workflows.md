# Development Commands

## Build and test

From the repository root:

```sh
dotnet restore TUnit.Dev.slnx
dotnet build TUnit.Dev.slnx --no-restore -graphBuild:True
```

Use `TUnit.slnx` for the full solution, including Roslyn version variants. Run `dotnet test` from the relevant test project directory. Core generator snapshots live in `tests/TUnit.Core.SourceGenerator.Tests`; public API snapshots live in `tests/TUnit.PublicAPI`.

For the intentionally failing test application, always select the class or method under test:

```sh
cd tests/TUnit.TestProject
dotnet test --treenode-filter "/*/*/ClassName/*"
```

A single-method filter has the form `/Assembly/Namespace/ClassName/TestMethodName`. Run one filter per command; do not join paths with `|`.

For AOT validation, publish the affected test application in Release with `-p:PublishAot=true --use-current-runtime`, selecting a target framework with `-f` when it multi-targets.

## Performance

Run `dotnet run -c Release` from the relevant benchmark project and compare before/after results:

- `benchmarks/TUnit.Performance.Tests`: core performance
- `benchmarks/TUnit.SourceGenerator.Benchmarks`: source generation
- `benchmarks/TUnit.PerformanceBenchmarks`: large-scale workloads

## Documentation snippets

CI compiles C# fences in `README.md` and `docs/docs` against packages produced by the pipeline, including fences nested in lists. Warnings are errors; failure-masking directives, warning pragmas, nullable disabling, `#if false`, and suppression attributes are rejected. `NoWarn` and `WarningsNotAsErrors` must be empty.

Place a directive immediately before a fence to specify its context:

```markdown
<!-- doc-test-declaration -->
<!-- doc-test-member -->
<!-- doc-test-statements -->
```

Use `<!-- doc-test-shared -->` once on tutorial pages whose fences share declarations; each fence still compiles in a page-scoped namespace. For fences mixing declarations or members with usage, split at an exact marker:

```markdown
<!-- doc-test-declaration: split-before=// Usage -->
<!-- doc-test-member: split-before=// Usage -->
```

Verify against local packages:

```powershell
./scripts/Verify-DocSnippets.ps1 -PackagesPath <package-directory> -Version <semver>
```
