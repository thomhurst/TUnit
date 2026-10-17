# Threat model

## What this project does and where untrusted input enters

TUnit is a .NET testing framework built on Microsoft.Testing.Platform. It ships as NuGet packages that test
authors reference from their test projects. Those packages include source generators and analyzers that run
inside the compiler and the IDE, a test engine, assertion and mocking libraries, reporters, and integrations
for ASP.NET Core, Aspire, OpenTelemetry and Playwright.

**Trust baseline.** TUnit's job is to compile and run test code. The test author's code, the project files,
MSBuild properties, command-line arguments and the developer's machine are all trusted. "A test can run
arbitrary code", "a malicious package or project file can compromise the build", or "a test can read
environment variables or secrets" are **not** vulnerabilities in TUnit.

TUnit is in scope where it crosses a boundary the test author does not control, or where it turns test data
into another language (HTML, Markdown, HTTP headers, file paths) that something else interprets. The data
crossing these boundaries includes test names, parameter values, captured output and exception messages. That
data often comes from the system under test, which may echo attacker-influenced content such as HTTP
responses, database rows, fuzzing input or third-party container logs.

The boundaries are:

1. **CI-facing output, rendered for other people.**
   - `src/TUnit.Engine/Reporters/GitHubReporter.cs` writes Markdown/HTML to `GITHUB_STEP_SUMMARY`.
   - `src/TUnit.Engine/Reporters/Aggregation/AggregatedSummaryWriter.cs` and `GitHubSummaryRegion.cs` merge
     results into a marker-delimited region of the step summary.
   - Test names, skip reasons, exception types/messages and filter strings flow into these.
   - Concerns: Markdown/HTML injection, breaking out of the marker region, spoofing other jobs' results.
2. **HTML report.**
   - `src/TUnit.Engine/Reporters/Html/HtmlReportGenerator.cs` and `TestReport.template.html` produce a
     self-contained HTML file that people open in a browser and that CI uploads as an artifact
     (`GitHubArtifactUploader.cs`).
   - Test names, IDs, output, exceptions, OpenTelemetry span names/attributes and source-link URLs end up in
     the DOM.
   - Concerns: XSS, `javascript:` or attacker-chosen URLs in links, `fetch` of attacker-chosen URLs.
3. **Cross-process report aggregation.**
   - `src/TUnit.Engine/Reporters/Aggregation/*` (`ReportAggregator`, `ReportDataJson`, `ReportDataMerger`,
     `AtomicFile`) has every test process read all `*.tunit-report.json` sidecars from a shared directory
     (`TUNIT_AGGREGATE_DIR`, or under `RUNNER_TEMP` on GitHub Actions) and regenerate a merged report.
   - The `tunit-report` dotnet tool (`src/TUnit.Reporting.Tool`) does the same recursively from a directory.
   - Treat sidecar contents as untrusted. Another job, matrix leg or an artifact download can produce them.
   - Concerns: parser crashes or resource exhaustion, values that become HTML/Markdown/URLs without
     validation, path handling and races in the shared directory.
4. **OTLP receiver (network listener).**
   - `src/TUnit.OpenTelemetry/Receiver/OtlpReceiver.cs` runs an unauthenticated `HttpListener` on
     `127.0.0.1` that accepts OTLP/HTTP protobuf from the system under test.
   - `src/TUnit.OpenTelemetry/AutoReceiver.cs` starts it automatically; `src/TUnit.Aspire.Core/AspireFixture.cs`
     starts it for Aspire apps.
   - `OtlpLogParser.cs` and `OtlpTraceParser.cs` contain a hand-written protobuf reader.
   - Parsed logs go into test output and spans into the HTML report. Request bodies are forwarded upstream
     with `OTEL_EXPORTER_OTLP_HEADERS` (which may carry API keys).
   - Any local process, and the containers and services under test, can send to it.
   - Concerns: memory-safety-like bugs in the parser (out-of-range reads, unbounded allocation, infinite
     loops), DoS, injecting spans/logs into another test's results, leaking forwarded credentials.
5. **Outbound HTTP header propagation.**
   - `src/TUnit.Core/HttpActivityPropagator.cs`, `TUnitActivitySource.cs` (baggage) and `PropagatorAlignment.cs`.
   - `src/TUnit.AspNetCore.Core/Http/TUnitHttpClientFilter.cs` adds `traceparent`, `baggage` and
     `X-TUnit-TestId` to HttpClients built inside the system under test.
   - `src/TUnit.Playwright/PlaywrightTraceContextRoute.cs` scopes Playwright trace headers to the origins
     under test.
   - Concerns: headers sent to third-party origins (information disclosure), header injection (CR/LF), origin
     matching bypasses.
6. **Inbound test correlation.**
   - `src/TUnit.AspNetCore.Core/Logging/TUnitTestContextMiddleware.cs` trusts the `X-TUnit-TestId` header and
     baggage to choose the current `TestContext`.
   - `Interception/HttpExchangeCaptureMiddleware.cs` captures request/response headers and bodies.
   - Concerns: cross-test data mixing, unbounded capture, secrets captured into reports.
7. **Files written from test-derived names.**
   - `src/TUnit.Engine/Helpers/PathValidator.cs` is used for report paths.
   - `src/TUnit.Playwright/PlaywrightVideoRecorder.cs` renames videos after test names.
   - The Aspire diagnostics log is written to the output directory.
   - Concerns: path traversal or overwriting files outside the intended directory via test names or sidecar
     data.
8. **Source generators, analyzers and code fixers** run in the IDE as soon as a project is opened, before the
   user chooses to build or run anything:
   - `src/TUnit.Core.SourceGenerator*`, `TUnit.Analyzers*`
   - `TUnit.Assertions.SourceGenerator`, `TUnit.Assertions.Should.SourceGenerator`, `TUnit.Assertions.Analyzers*`
   - `TUnit.Mocks.SourceGenerator*`, `TUnit.Mocks.Analyzers`
   - `TUnit.AspNetCore.Analyzers*`

   Concerns:
   - Generated code whose meaning differs from the source because of incorrect escaping of string literals or
     identifiers (code injection through attribute arguments or names).
   - Crashes or hangs on unusual syntax.

## Components that matter most / least

**Most important:**
- the OTLP receiver and its protobuf parsers;
- the HTML report (generator plus template);
- the GitHub step-summary writers;
- report aggregation and `tunit-report`;
- header propagation and Playwright origin scoping;
- source generator escaping.

**Also in scope:**
- `TUnit.Engine` execution, filtering and data-source handling;
- `TUnit.Assertions`, where regex and JSON assertions take test-author input, so ReDoS here is low severity
  at most;
- `TUnit.Mocks`, `TUnit.Mocks.Http`;
- `TUnit.AspNetCore*`, `TUnit.Aspire*`;
- `TUnit.Mocks.InternalsAccess.Tasks`, an MSBuild task that rewrites referenced assemblies with Mono.Cecil at
  build time.

**Out of scope:**
- `tests/`, `benchmarks/`, `examples/`, `docs/`, `tools/` and `TUnit.Templates` content. These are
  development-only and not shipped as runtime code.
- `tests/TUnit.TestProject` contains deliberately failing tests.
- `.github/workflows` is out of scope for this scan.
- Microsoft.Testing.Platform itself (server mode, IPC, command-line parsing) is an upstream dependency.
  Report issues only where TUnit's use of it is wrong.

## How to exercise it

- The image is built in Debug from `TUnit.CI.slnx`, plus `src/TUnit.Reporting.Tool`.
- Test projects are TUnit apps. Run them with
  `cd tests/<Project> && dotnet run -c Debug --no-build --framework net10.0`. net8.0 and net9.0 runtimes are
  also installed.
- `tunit-oss-test` runs the offline-safe suites for the areas above.
- Relevant suites:
  - `tests/TUnit.UnitTests`: aggregation, sidecar JSON, step summary, path validation, Playwright origin
    route.
  - `tests/TUnit.OpenTelemetry.Tests`: OTLP receiver and parsers.
  - `tests/TUnit.AspNetCore.Tests`: header propagation, capture, test-ID middleware.
  - `tests/TUnit.Engine.Tests`: end-to-end reporters. These invoke `tests/TUnit.TestProject` with filters.
- To produce a report from arbitrary test names and output, add a test with a hostile name or output in a
  scratch TUnit project, or run one class of `tests/TUnit.TestProject` with
  `--treenode-filter "/*/*/<ClassName>/*" --report-html`.
- **Never run `tests/TUnit.TestProject` without `--treenode-filter`.**
- Set `GITHUB_ACTIONS=true` and `GITHUB_STEP_SUMMARY=<file>` to exercise the GitHub reporter.
- Set `TUNIT_AGGREGATE_DIR=<dir>` to exercise aggregation.
- The OTLP receiver listens on a random loopback port when `TUnit.OpenTelemetry` is referenced. POST
  protobuf to `/v1/logs` or `/v1/traces`.
- **Not available offline:**
  - `tests/TUnit.Aspire.Tests` needs Docker.
  - Playwright tests need downloaded browsers.
  - The GitHub artifact uploader needs GitHub's results service.
  - Review these by reading the code.

## How you rate severity

These are developer and CI tools. Rate by realistic impact on a developer machine or CI runner.

- **Critical:**
  - Code execution, or theft of CI secrets (`ACTIONS_RUNTIME_TOKEN`, OTLP API keys, cloud credentials), from
    input the test author does not control: network data, sidecar files from another job, or system-under-test
    output that TUnit renders.
  - Code injection through source generator output from ordinary source constructs.
- **High:**
  - Stored XSS in the HTML report or merged report from test data or sidecar data.
  - Markdown/HTML injection in the GitHub step summary that can spoof results or hide failures.
  - Sending trace or baggage headers to origins outside the configured set.
  - Arbitrary file write or overwrite outside the intended output directory.
- **Medium:**
  - Unauthenticated loopback OTLP input that corrupts or spoofs another test's results.
  - Crashes or unbounded memory/CPU in the OTLP parser or sidecar reader from malformed input.
  - Secrets captured into reports by default.
- **Low:**
  - Crashes or hangs in generators or analyzers on unusual but valid code.
  - ReDoS from patterns supplied by the test author.
  - Issues that need the attacker to already control the test project, the environment variables or the
    command line.
- Denial of service against a single test run is at most medium.

## Anything to leave alone

- The trust-baseline items above: tests and fixtures executing code, reading the environment, writing files
  or making network calls are by design.
- Reflection, dynamic code or `Activator` use in the reflection execution mode is by design. AOT and trimming
  warnings are not security issues.
- Loopback `HttpListener` binding without authentication is a known design choice. Report concrete abuse, not
  the absence of auth itself.
- Missing timeouts on regexes built from test-author patterns, unless the pattern can come from untrusted
  input.
- Performance or allocation findings with no security impact.

## How we would like reports

- Include a minimal reproducer: a test, a sidecar JSON file, or a protobuf payload.
- Name the affected package(s) and file(s).
- Propose a patch against `main` with a regression test in the matching `tests/` project.
- Use Microsoft.Testing.Platform APIs, never VSTest APIs.
- Keep fixes Native AOT and trimming compatible.
