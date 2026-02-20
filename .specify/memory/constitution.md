<!--
Sync Impact Report
===================
Version change: [template] → 1.0.0
Modified principles: N/A (initial creation)
Added sections:
  - Core Principles (6 principles)
  - Technical Constraints
  - Development Workflow
  - Governance
Removed sections: None
Templates requiring updates:
  - .specify/templates/plan-template.md ✅ no updates needed (Constitution Check is generic)
  - .specify/templates/spec-template.md ✅ no updates needed (structure-agnostic)
  - .specify/templates/tasks-template.md ✅ no updates needed (structure-agnostic)
Follow-up TODOs: None
-->

# TUnit Constitution

## Core Principles

### I. Performance First

TUnit processes millions of tests across diverse project types.
Performance is a non-negotiable constraint, not an afterthought.

- All hot paths (test discovery, execution, data generation)
  MUST minimize allocations.
- Reflection results MUST be cached; redundant lookups are
  prohibited.
- `ValueTask` MUST be used for operations that may complete
  synchronously.
- Object pooling MUST be applied for frequently allocated
  objects in critical paths.
- Performance-impacting changes MUST be profiled before and
  after merge.

**Rationale**: Users run TUnit against codebases of all sizes.
Slow test frameworks erode developer trust and productivity.

### II. Intuitive & Easy-to-Use API

TUnit MUST be approachable for newcomers while remaining
powerful for advanced users.

- Public APIs MUST follow the principle of least surprise;
  naming, parameter ordering, and defaults MUST be consistent.
- New features MUST include XML documentation on all public
  members.
- The fluent assertion API (`Assert.That(...)`) is the primary
  user-facing surface and MUST remain discoverable and readable.
- Error messages MUST clearly describe what was expected, what
  was received, and (where possible) how to fix the problem.
- `[GenerateAssertion]` MUST be preferred over manual assertion
  patterns to reduce boilerplate for contributors.

**Rationale**: A testing framework that is hard to learn or
produces cryptic failures will not be adopted or trusted.

### III. Flexibility & Extensibility

TUnit serves many project types (libraries, web apps, mobile,
cloud, desktop). It MUST NOT impose unnecessary constraints.

- The attribute-based test model MUST support parameterization,
  data sources, and custom lifecycle hooks without requiring
  users to subclass framework types.
- Integration packages (ASP.NET Core, Playwright, Aspire, etc.)
  MUST be optional and independently consumable.
- Users MUST be able to extend TUnit through public interfaces
  and extension points rather than forking internal code.
- New features MUST NOT break existing user test suites unless
  accompanied by a documented migration path.

**Rationale**: A rigid framework cannot serve the breadth of
.NET project types that TUnit targets.

### IV. Dual-Mode Compatibility (NON-NEGOTIABLE)

TUnit supports two test discovery modes that MUST produce
identical behavior:

- **Source-Generated Mode** (`TUnit.Core.SourceGenerator`):
  compile-time code generation.
- **Reflection Mode** (`TUnit.Engine`):
  runtime test discovery.

Rules:
- Any change to core engine metadata collection MUST be
  implemented in both modes.
- Tests MUST verify that both modes behave identically.
- Post-metadata-collection code (unified path) is exempt.

**Rationale**: Source generation enables AOT and faster startup;
reflection enables dynamic scenarios. Both MUST be first-class.

### V. AOT & Modern .NET Compatibility

All code MUST work with Native AOT compilation and IL trimming.

- Reflection usage MUST be annotated with
  `[DynamicallyAccessedMembers]`.
- Warnings MUST only be suppressed with
  `[UnconditionalSuppressMessage]` and a justification.
- Modern C# language features (`LangVersion preview`) MUST be
  used; legacy patterns are prohibited when a modern alternative
  exists.
- Multi-targeting (`net8.0;net9.0;net10.0`) MUST be maintained.

**Rationale**: AOT is the future of .NET deployment. TUnit MUST
not be the dependency that prevents users from adopting it.

### VI. Quality Gates

Every change MUST pass established quality gates before merge.

- Snapshot tests MUST be run when changing source generator
  output or public APIs. `.verified.txt` files MUST be committed;
  `.received.txt` files MUST NEVER be committed.
- `Microsoft.Testing.Platform` is the ONLY supported test
  platform. `Microsoft.VisualStudio.TestPlatform` (VSTest) MUST
  NEVER be referenced.
- Async code MUST NEVER be blocked with `.Result` or
  `.GetAwaiter().GetResult()`.
- `TUnit.TestProject` MUST NEVER be run without filters (many
  tests are designed to fail).

**Rationale**: Automated quality gates prevent regressions and
enforce consistency across a large contributor base.

## Technical Constraints

- **.NET SDK**: 10.0+ required (enforced by `global.json`).
- **Target Frameworks**: `net8.0;net9.0;net10.0` multi-target.
- **Language Version**: `preview` (use latest C# features).
- **Test Platform**: `Microsoft.Testing.Platform` exclusively.
- **Roslyn Compatibility**: Multi-target across Roslyn versions
  (4.14, 4.4, 4.7) for source generators and analyzers.
- **No blocking on async**: No `.Result`, no
  `.GetAwaiter().GetResult()`.

## Development Workflow

- **Snapshot Testing**: Run `dotnet test` on
  `TUnit.Core.SourceGenerator.Tests` and `TUnit.PublicAPI` when
  changing generated output or public API surface.
- **Filtered Test Execution**: Use `--treenode-filter` when
  running `TUnit.TestProject` to avoid executing intentionally
  failing tests.
- **Dual-Mode Verification**: Changes to metadata collection
  MUST include tests that exercise both source-gen and reflection
  modes using `[Arguments(ExecutionMode.SourceGenerated)]` and
  `[Arguments(ExecutionMode.Reflection)]`.
- **AOT Verification**: Publish with
  `-p:PublishAot=true --use-current-runtime` to verify trimming
  compatibility.

## Governance

This constitution is the authoritative source for TUnit's
development principles and constraints. It supersedes all other
guidance when conflicts arise.

- **Amendments**: Any change to this constitution MUST be
  documented with a version bump, rationale, and migration plan
  if existing workflows are affected.
- **Versioning**: This document follows semantic versioning:
  - MAJOR: Principle removal or backward-incompatible redefinition.
  - MINOR: New principle or materially expanded guidance.
  - PATCH: Clarifications, wording, or non-semantic refinements.
- **Compliance**: All pull requests and reviews MUST verify
  compliance with these principles. Violations MUST be justified
  in the PR description if an exception is warranted.
- **Runtime Guidance**: `CLAUDE.md` and `.claude/docs/` contain
  implementation-level guidance that MUST align with this
  constitution.

**Version**: 1.0.0 | **Ratified**: 2026-02-20 | **Last Amended**: 2026-02-20
