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
- Do not run `tests/TUnit.Engine.Tests` locally: it takes a long time and uses a lot of resources. Rely on CI for it.
- C# documentation fences are compiled in CI with warnings as errors. See [workflows](.claude/docs/workflows.md#documentation-snippets) when editing them.

## Git workflow

- Use a feature branch and a ready-for-review PR by default; create drafts only when requested. Trivial documentation, agent-instruction, comment, and typo changes may go directly to `main`.
- For continuous, unattended issue and PR work, use the [issue-pr-loop skill](.agents/skills/issue-pr-loop/SKILL.md) (`/issue-pr-loop`, or `pwsh scripts/Start-IssuePrLoop.ps1` for a fresh agent per unit). PRs it opens follow its draft review lifecycle.
- Use `pwsh scripts/Remove-MergedWorktrees.ps1` for squash-safe merged-worktree cleanup; it also removes checkouts of closed `pr-<N>`/`issue-<N>` items, keeping their branch, and saves the uncommitted work of merged or closed checkouts under `retained-worktrees/` (unknown ignored files still keep a checkout). `-WhatIf` reports removal candidates and preserved files.
- When asked to clean up worktrees, remove those for merged or `[gone]` branches without per-item confirmation. Preserve branches unless deletion is requested.
- Verify review findings against the code before fixing them. If a reviewer or user reaffirms a disputed finding after one reasoned objection, implement it.
- Every review cycle, check both review threads (`pulls/<N>/reviews`, `pulls/<N>/comments`, GraphQL `reviewThreads`) and PR conversation comments (`issues/<N>/comments`). Reviewers differ: Claude often posts its review as a conversation comment, while other agents use review threads. A finding on either surface needs a disposition before merge.
- Resolve each PR review thread, whether a human or a bot opened it, as soon as you have dispositioned it: the fix is pushed to the PR head and your reply names the commit, or your reply pushes back on the finding with evidence. Leave a thread open only while it has no disposition. If the reviewer replies after your disposition, unresolve the thread and handle the reply.

## References

Read only as needed:

- [Build, test, benchmark, and documentation commands](.claude/docs/workflows.md)
- [Architecture and source locations](.claude/docs/project-structure.md)

## Local worktree lifecycle

Use `scripts/AgentLocks.ps1` from the shared checkout for work-item ownership (`pr-<N>` or `issue-<N>`). Set `$agentLocks` to its absolute path, acquire before creating the checkout, and pass the same stable `-OwnerId` to every verb (`acquire`, `renew`, `release`) outside Codex, or set `TUNIT_AGENT_LOCK_OWNER_ID`. Git and Docker are required locally.

On Windows, before creating the first worktree, run `git config --local core.longpaths true` from the shared checkout. Template snapshot paths can exceed Windows' `MAX_PATH` under the worktree root. This repository-local setting is shared by its worktrees and must be enabled before checkout; see [Windows setup](scripts/WorktreeLifecycle.md#windows-setup).

Immediately after checkout, run `pwsh $agentLocks renew -LockName $lockName -Worktree $worktree -OwnerId $ownerId` once to register its path. Stop owned processes, save needed evidence under the evidence root (below), then release from the shared checkout in `finally`. Release removes clean checkouts even for open PRs; branches and detached commits remain available for re-checkout. See [local worktree lifecycle](scripts/WorktreeLifecycle.md) for preservation rules and crash behavior.

Never edit the main checkout, and write nothing loose beside it. Every file an agent produces outside a worktree has one home, so cleanup can find it:

- Evidence (build and test logs, reports, PR and comment body drafts, diffs, validation projects) goes in `<repo-parent>/TUnit-evidence/<lock-name>/`, for example `C:/git/TUnit-evidence/issue-1234/build.log`. `pwsh scripts/Remove-MergedWorktrees.ps1` deletes a `pr-<N>` or `issue-<N>` entry once that item is closed (merged or not) and no registered worktree uses it, and any entry once its newest file is older than `-EvidenceRetentionDays` (default 7). Release keeps the evidence of an open item; upload anything that must outlive the item.
- Worktrees, including throwaway checkouts and baselines, go under the dedicated worktree root `<repo-parent>/TUnit-worktrees/<name>`, never as plain copies. It is the only root whose orphaned directories the sweep cleans.
- Downloaded SDKs and CLI tools go in `<repo-parent>/toolchains/<name>-<version>/`, shared by every repository.
- Never write to the repository's parent directory itself or to the main checkout.

## Autonomous self-improvement

Treat every problem you notice as work to act on, whether you find it while testing, developing, reading code, reviewing, or doing anything else. Examples include bugs, flaky or failing tests, incorrect docs, misleading comments, broken tooling, and gaps in these instructions.

- If the fix is trivial and low-risk, fix it as part of the current work and mention it in the PR description.
- If the fix is out of scope for the current work, raise a GitHub issue at the point you discover it so it can be fixed later. Include what you saw, where you saw it (file and line, command, or CI run), how to reproduce it, and the expected behavior. Search existing issues first, and add to a matching issue instead of opening a duplicate. Write it as [Writing issues](#writing-issues) describes.
- Do not ignore a problem because it existed before your change or is unrelated to it.

### Writing issues

Every issue must be understood by a reader who has never seen TUnit, from its title and first paragraph alone. This matters most in another project's repository: its maintainers do not know our code, terms or constraints, and an issue they cannot follow gets ignored or closed.

- **Title:** the observed problem or the requested capability in that project's own API terms, for example "`BasicPublishAsync` rejects `ReadOnlyMemory<byte>` header values". No TUnit names or shorthand.
- **Summary first:** open with two or three plain sentences: what happens, what you expected, and why a user of that project would care. Details come after.
- **Their vocabulary, not ours:** do not use TUnit's internal type, component or decision names, or our own targets and budgets; say what the code does instead. Use the project's issue template when it has one.
- **Short sections:** Summary, Reproduction, Expected and actual, Environment (exact versions); for a feature, Proposal instead of Expected and actual. The reproduction is minimal and self-contained, never needs TUnit, and source links are pinned to the tag you checked.
- **One ask, stated once:** one problem per issue. End with a direct question or request, not a menu of options.
- **Cut what the maintainer does not need to act:** investigation narrative, hedging, our workarounds, local paths, worktree names, internal CI details. Mention TUnit once, at the end, with a link to the tracking issue.
- **Check before filing:** search the project's open and closed issues, and its `main` branch and unreleased release notes, not only the version we use.
- **Reread as the maintainer:** could they tell within 30 seconds what is wrong and what you are asking? If a maintainer says an issue is unclear, rewrite the body in place, then reply with a short plain summary.
