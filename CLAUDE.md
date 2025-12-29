# TUnit Development Guide

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
dotnet run -- --treenode-filter "/*/*/SpecificClass/*"
```
See `.claude/docs/workflows.md` for filter syntax and details.

## Quick Fix: Snapshot Tests Failing

```bash
# Review changes, then accept if intentional:
# Linux/macOS:
for f in *.received.txt; do mv "$f" "${f%.received.txt}.verified.txt"; done

# Windows:
for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"

git add *.verified.txt
```

## Code Principles

- **Use modern C# and .NET features.** Prefer latest syntax and APIs.
- **Prefer `[GenerateAssertion]`** for new assertions. See `.claude/docs/patterns.md`.
- **NEVER block on async** - No `.Result` or `.GetAwaiter().GetResult()`.

## Decision Framework

> "Does this make TUnit faster, more modern, more reliable, or more enjoyable to use?"

## Further Documentation

- `.claude/docs/mandatory-rules.md` - Full rule details
- `.claude/docs/workflows.md` - Commands, checklists, filters
- `.claude/docs/patterns.md` - Code examples
- `.claude/docs/project-structure.md` - Project map
- `.claude/docs/troubleshooting.md` - Common issues
