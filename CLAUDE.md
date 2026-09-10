# TUnit Agent Guide

Use the SDK selected by `global.json`. Shared build settings live in `Directory.Build.props` (including preview C# and target frameworks).

## Project constraints

- Core engine metadata collection changes must behave identically in `src/TUnit.Core.SourceGenerator` and `src/TUnit.Engine`; test both modes. Execution after metadata collection uses a shared path, so this does not require duplicate implementations for assertions, analyzers, or shared execution code.
- Use `Microsoft.Testing.Platform`, not VSTest APIs (`Microsoft.VisualStudio.TestPlatform`).
- Preserve Native AOT and trimming compatibility. Annotate reflection as needed and verify AOT publishing when changing reflection paths.
- Minimize allocations and cache reflection in discovery, execution, and data-generation hot paths. Prefer `ValueTask` for potentially synchronous operations; measure before and after performance changes.
- Do not block on async. Prefer `[GenerateAssertion]` for new assertions; use existing implementations in `src/TUnit.Assertions` as examples.

## Validation

- Run tests relevant to the change before pushing code.
- Generator output changes require the affected generator's snapshot tests; public API changes require `tests/TUnit.PublicAPI`. Review snapshot differences, commit intentional updates as `.verified.txt`, and never commit `.received.txt`.
- Never run `tests/TUnit.TestProject` without a targeted `--treenode-filter`: it contains intentional failures. Run separate filters rather than joining paths with `|`, which can match unintended tests.
- C# documentation fences are compiled in CI with warnings as errors. See [workflows](.claude/docs/workflows.md#documentation-snippets) when editing them.

## Git workflow

- Use a feature branch and a ready-for-review PR by default; create drafts only when requested. Trivial documentation, agent-instruction, comment, and typo changes may go directly to `main`.
- Use `pwsh scripts/Remove-MergedWorktrees.ps1` for squash-safe merged-worktree cleanup; `-WhatIf` reports removal candidates and preserved files.
- When asked to clean up worktrees, remove those for merged or `[gone]` branches without per-item confirmation. Preserve branches unless deletion is requested.
- Verify review findings against the code before fixing them. If a reviewer or user reaffirms a disputed finding after one reasoned objection, implement it.

## References

Read only as needed:

- [Build, test, benchmark, and documentation commands](.claude/docs/workflows.md)
- [Architecture and source locations](.claude/docs/project-structure.md)

## Local worktree lifecycle

Use `scripts/AgentLocks.ps1` from the shared checkout for work-item ownership (`pr-<N>` or `issue-<N>`). Set `$agentLocks` to its absolute path, acquire before creating the checkout, and use a stable `-OwnerId` outside Codex. Git and Docker are required locally.

Immediately after checkout, run `pwsh $agentLocks renew -LockName $lockName -Worktree $worktree` once to register its path. Stop owned processes, archive needed evidence outside the worktree, then release from the shared checkout in `finally`. Release removes clean checkouts even for open PRs; branches and detached commits remain available for re-checkout. See [local worktree lifecycle](scripts/WorktreeLifecycle.md) for preservation rules and crash behavior.
