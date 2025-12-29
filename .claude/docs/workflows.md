# Development Workflows

Commands, processes, and checklists for TUnit development.

---

## Common Commands

```bash
# Run all tests (excludes TUnit.TestProject)
dotnet test

# Snapshot tests
dotnet test TUnit.Core.SourceGenerator.Tests
dotnet test TUnit.PublicAPI

# Run specific test
dotnet test -- --treenode-filter "/*/*/ClassName/*"

# Build release
dotnet build -c Release

# Test AOT compatibility
dotnet publish -c Release -p:PublishAot=true --use-current-runtime
```

---

## TUnit.TestProject Filters

Many tests in `TUnit.TestProject` are designed to fail (testing error scenarios). Always use filters.

### Filter Syntax

```bash
# Single test
--treenode-filter "/TUnit.TestProject/Namespace/ClassName/TestMethodName"

# All tests in a class
--treenode-filter "/*/*/ClassName/*"

# Exclude by category
--treenode-filter "/*/*/*/*[Category!=Performance]"
```

### Run Filters One at a Time

OR patterns (`Pattern1|Pattern2`) can match thousands of unintended tests.

```bash
# Wrong - matches too broadly
--treenode-filter "/*/*/ClassA/*|/*/*/ClassB/*"

# Correct - separate commands
dotnet run -- --treenode-filter "/*/*/ClassA/*"
dotnet run -- --treenode-filter "/*/*/ClassB/*"
```

---

## Adding a New Feature

1. Write tests FIRST (TDD)
2. Does it change core engine metadata collection?
   - YES: Implement in BOTH source-gen AND reflection
   - NO: Use unified code path
3. Implement in `TUnit.Core` if new abstractions needed
4. Add analyzer rule if misuse is possible
5. Run all tests: `dotnet test`
6. Accept snapshots if needed (see CLAUDE.md)
7. Benchmark if touching hot paths
8. Test AOT if using reflection

---

## Fixing a Bug

1. Write failing test that reproduces the bug
2. Identify if it affects metadata collection (dual-mode)
3. Fix in both source generator and reflection engine if needed
4. Run full test suite: `dotnet test`
5. Accept snapshots if applicable

---

## Pre-Commit Checklist

- [ ] All tests pass: `dotnet test`
- [ ] If source generator changed: ran snapshot tests, committed `.verified.txt`
- [ ] If public API changed: ran `TUnit.PublicAPI`, committed `.verified.txt`
- [ ] If dual-mode feature: implemented in both modes, tested both
- [ ] If performance-critical: profiled before/after
- [ ] If using reflection: tested AOT, added annotations
- [ ] No `.received.txt` files staged
