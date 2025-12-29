# TUnit Development Guide

## CRITICAL RULES

These rules are non-negotiable. See `.claude/docs/mandatory-rules.md` for full details.

1. **Dual-Mode Implementation** - Changes to core engine metadata collection MUST work in both source-gen (`TUnit.Core.SourceGenerator`) AND reflection (`TUnit.Engine`) modes identically.

2. **Snapshot Testing** - Any change to source generator output or public APIs requires running snapshot tests and committing `.verified.txt` files. NEVER commit `.received.txt` files.

3. **No VSTest** - Use `Microsoft.Testing.Platform` only. NEVER use `Microsoft.VisualStudio.TestPlatform`.

4. **Performance First** - Minimize allocations in hot paths. Cache reflection results. Use `ValueTask` for potentially-sync operations. Profile before/after for critical path changes.

5. **AOT/Trimming Compatible** - All code must work with Native AOT. Annotate reflection usage with `[DynamicallyAccessedMembers]`.

## IMPORTANT WARNINGS

**NEVER run `TUnit.TestProject` without filters.** Many tests are designed to fail. Always use:
```bash
dotnet run -- --treenode-filter "/*/*/SpecificClass/*"
```
See `.claude/docs/workflows.md` for details.

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

- **Use modern C# and .NET features.** Prefer latest syntax and APIs over legacy patterns.
- **Prefer `[GenerateAssertion]`** for new assertions. Use manual patterns only for complex cases. See `.claude/docs/patterns.md`.
- **NEVER block on async** - No `.Result` or `.GetAwaiter().GetResult()`.

## Decision Framework

Before any change, ask:

> "Does this make TUnit faster, more modern, more reliable, or more enjoyable to use?"

If NO, reconsider the change.

## Documentation Map

| Topic | File |
|-------|------|
| Full rule details | `.claude/docs/mandatory-rules.md` |
| Commands & workflows | `.claude/docs/workflows.md` |
| Code patterns | `.claude/docs/patterns.md` |
| Project structure | `.claude/docs/project-structure.md` |
| Troubleshooting | `.claude/docs/troubleshooting.md` |

## Resources

- Documentation: https://tunit.dev
- Issues: https://github.com/thomhurst/TUnit/issues
- Contributing: `.github/CONTRIBUTING.md`
