# TUnit Development Guide

## Environment Requirements

- .NET SDK 10.0+ (required by `global.json`; multi-targets `net8.0;net9.0;net10.0`)

## CRITICAL RULES

1. **Dual-Mode** - Changes to core engine metadata collection MUST work in both source-gen (`TUnit.Core.SourceGenerator`) AND reflection (`TUnit.Engine`) modes.

2. **Snapshot Testing** - Changes to source generator output or public APIs require running snapshot tests. Commit `.verified.txt` files. NEVER commit `.received.txt`.

3. **No VSTest** - Use `Microsoft.Testing.Platform` only. NEVER use `Microsoft.VisualStudio.TestPlatform`.

4. **Performance First** - Minimize allocations in hot paths. Cache reflection. Use `ValueTask` for potentially-sync operations.

5. **AOT Compatible** - All code must work with Native AOT. Annotate reflection with `[DynamicallyAccessedMembers]`.

See `.claude/docs/mandatory-rules.md` for full details.

## IMPORTANT WARNINGS

**NEVER run `TUnit.TestProject` without filters.** Many tests are designed to fail.
```bash
cd TUnit.TestProject
dotnet test --treenode-filter "/*/*/SpecificClass/*"
```
See `.claude/docs/workflows.md` for filter syntax and details.

## Quick Fix: Snapshot Tests Failing

```bash
# Review changes, then accept if intentional:
# (Run from test project directory, e.g., TUnit.Core.SourceGenerator.Tests)

# Linux/macOS:
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

# Windows:
for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"

git add *.verified.txt
```

## Code Principles

- **Use modern C# and .NET features.** `LangVersion` is `preview` — use latest syntax and APIs.
- **Prefer `[GenerateAssertion]`** for new assertions. See `.claude/docs/patterns.md`.
- **NEVER block on async** - No `.Result` or `.GetAwaiter().GetResult()`.

## Decision Framework

> "Does this make TUnit faster, more modern, more reliable, or more enjoyable to use?"

## Branch & PR Workflow

- Default: create a feature branch, open a PR, iterate via review feedback. Don't push code changes directly to `main`.
- Exception — direct push to `main` is fine for trivial, low-risk changes that don't need review: doc tweaks (README, CLAUDE.md, `.claude/**`), agent-instruction updates, comment-only edits, typo fixes. Use judgment; if in doubt, branch + PR.
- If the user says "push to main" while currently on `main`, confirm intent: do they mean "push my branch and merge", or "push the current branch which happens to be `main`"?

## Worktree Cleanup

- When asked to clean up worktrees, proceed with `git worktree remove` for `[gone]` or merged branches without asking for per-item confirmation; report what was done.
- Preserve branches by default — only delete when explicitly asked.

## PR Review Iteration

- Verify each review finding against the code before applying a fix — don't blindly accept reviewer suggestions.
- Run tests locally before pushing.
- If you disagree with a review item, push back **once** with concrete reasoning. If the reviewer or user reaffirms, implement it instead of continuing to argue.
- Before commit, check that related tests, snapshots, and downstream files in the same module were updated alongside the source change.

## Output Limits

- Keep individual responses under the 500-token output limit. For long results, split across turns or write to a file and reference it.

## Further Documentation

- `.claude/docs/mandatory-rules.md` - Full rule details
- `.claude/docs/workflows.md` - Commands, checklists, filters
- `.claude/docs/patterns.md` - Code examples
- `.claude/docs/project-structure.md` - Project map
- `.claude/docs/troubleshooting.md` - Common issues
