# Development Commands

## Build and test

From the repository root:

```sh
dotnet restore TUnit.Dev.slnx
dotnet build TUnit.Dev.slnx --no-restore -graphBuild:True
```

Use `TUnit.slnx` for the full solution, including Roslyn version variants. Run `dotnet test` from the relevant test project directory. Core generator snapshots live in `tests/TUnit.Core.SourceGenerator.Tests`; public API snapshots live in `tests/TUnit.PublicAPI`.

Test projects target net8.0, net9.0 and net10.0; only installed runtimes can run, so pass `--framework net10.0` (or another installed one) when the machine lacks the others. The PublicAPI and generator snapshot projects add a net472 leg on Windows only; off Windows, `.Net4_7` snapshot updates come from the `ReceivedSnapshots-windows-latest` CI artifact.

Scripts in `scripts/` need PowerShell 7. On Linux and macOS, quote any argument containing `*` when calling a native command from PowerShell: unquoted arguments are glob-expanded, so a treenode filter becomes a list of file paths.

For the intentionally failing test application, always select the class or method under test:

```sh
cd tests/TUnit.TestProject
dotnet test --framework net10.0 --treenode-filter "/*/*/ClassName/*"
```

A single-method filter has the form `/Assembly/Namespace/ClassName/TestMethodName`. Run one filter per command; do not join paths with `|`.

For AOT validation of `tests/TUnit.TestProject`, publish in Release with `-p:Aot=true --use-current-runtime`, selecting a target framework with `-f` when it multi-targets. This project switch scopes `PublishAot` to supported target frameworks; passing `-p:PublishAot=true` globally also applies it to `netstandard2.0` analyzer project references and fails with NETSDK1207.

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

## Native macOS crashes

When the repository variable `CRASH_DUMP_ENCRYPTION_CERT` contains a PEM X.509
recipient certificate with an RSA public key, the macOS modular pipeline enables
.NET crash dumps, encrypts them with OpenSSL CMS (AES-256), and uploads only the
`.cms` files as `EncryptedNativeCrashDumps-macos-latest`, retained for seven days.
Keep the matching private key outside CI. Without the variable, capture is disabled.
Encryption failure fails the step; raw dumps are deleted and never uploaded.
To decrypt a downloaded artifact locally:

```sh
openssl cms -decrypt -binary -inform DER -in crash.dmp.cms \
  -recip recipient.pem -inkey recipient.key -out crash.dmp
```

Pipeline processes inherit credentials, so decrypted dumps must remain private. An exit such as 139
before test results can be a native runtime fault; keep the failing exit code
and inspect the dump rather than treating a passing rerun as a fix.

To capture a recurrence locally, use an absolute evidence directory outside the
checkout (replace the example path with your item’s evidence directory):

```sh
mkdir -p /absolute/path/TUnit-evidence/issue-7434
DOTNET_DbgEnableMiniDump=1 \
DOTNET_DbgMiniDumpType=2 \
DOTNET_DbgMiniDumpName=/absolute/path/TUnit-evidence/issue-7434/crash.%e.%p.dmp \
dotnet test --project tests/TUnit.UnitTests --framework net10.0
```

Type 2 includes heap information for managed and native diagnosis. Preserve the
SDK/runtime versions, commit, command, exit code, and macOS DiagnosticReports
stack with the dump. Dumps can contain process memory; review them before
sharing outside the repository. A matching native stack alone does not establish
that an older upstream runtime bug is the cause.
