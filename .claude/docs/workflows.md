# Development Workflows

## Common Commands

```bash
# Run all tests (excludes TUnit.TestProject integration tests)
dotnet test

# Test source generator + accept snapshots
dotnet test TUnit.Core.SourceGenerator.Tests

# Test public API + accept snapshots
dotnet test TUnit.PublicAPI

# Run specific test
dotnet test -- --treenode-filter "/Assembly/Namespace/ClassName/TestName"

# All tests in a class
dotnet test -- --treenode-filter "/*/*/ClassName/*"

# Exclude performance tests
dotnet test -- --treenode-filter "/*/*/*/*[Category!=Performance]"

# Build release
dotnet build -c Release

# Test AOT compatibility
dotnet publish -c Release -p:PublishAot=true --use-current-runtime
```

---

## TUnit.TestProject Warning

IMPORTANT: **NEVER run `TUnit.TestProject` without filters.**

Many tests are intentionally designed to fail to verify error handling. Running without filters will show many "failures" that are expected behavior.

### Correct Usage

```bash
# Always use targeted filters
cd TUnit.TestProject
dotnet run -- --treenode-filter "/*/*/SpecificClass/*"
dotnet run -- --treenode-filter "/*/*/*/*[Category!=Performance]"
```

### Why Tests "Fail"

TUnit.TestProject contains:
- Tests that verify failure scenarios (expected to fail)
- Tests for error messages and diagnostics
- Performance tests that should be excluded by default
- Integration tests covering edge cases

### Filter Syntax

```bash
# Single test
--treenode-filter "/TUnit.TestProject/Namespace/ClassName/TestMethodName"

# All tests in a class
--treenode-filter "/*/*/ClassName/*"

# Exclude by category
--treenode-filter "/*/*/*/*[Category!=Performance]"
```

IMPORTANT: **Run filters ONE AT A TIME.** OR patterns (`Pattern1|Pattern2`) can match thousands of unintended tests.

```bash
# WRONG - OR patterns match too broadly
--treenode-filter "/*/*/ClassA/*|/*/*/ClassB/*"

# CORRECT - Run separate commands
dotnet run -- --treenode-filter "/*/*/ClassA/*"
dotnet run -- --treenode-filter "/*/*/ClassB/*"
```

---

## Adding a New Feature

- Is this a new feature?
  - Does it change core engine metadata collection?
    - YES: Implement in BOTH source-gen AND reflection (see Rule 1)
    - NO: Use unified code path
  - Does it change public API?
    - YES: Run `TUnit.PublicAPI` tests, accept snapshots
  - Does it change source generator output?
    - YES: Run `TUnit.Core.SourceGenerator.Tests`, accept snapshots
  - Does it touch hot paths?
    - YES: Profile before/after, benchmark
  - Does it use reflection?
    - YES: Test with AOT, add `[DynamicallyAccessedMembers]`

### Steps

1. Write tests FIRST (TDD)
2. Implement in `TUnit.Core` (if new abstractions needed)
3. Implement in source-gen/reflection if metadata collection (see Rule 1)
4. Add analyzer rule if misuse is possible
5. Run all tests: `dotnet test`
6. Accept snapshots if needed
7. Benchmark if touching hot paths
8. Test AOT if using reflection

---

## Fixing a Bug

1. Write failing test that reproduces the bug
2. Identify if it affects metadata collection (dual-mode)
3. Fix in source generator (if affected)
4. Fix in reflection engine (if affected)
5. Verify both modes pass the test
6. Run full test suite: `dotnet test`
7. Accept snapshots if applicable
8. Check for performance regression if in hot path

---

## Pre-Commit Checklist

Before committing, verify:

- All tests pass: `dotnet test`
- If source generator changed:
  - Ran `TUnit.Core.SourceGenerator.Tests`
  - Reviewed and accepted `.verified.txt` snapshots
- If public API changed:
  - Ran `TUnit.PublicAPI` tests
  - Reviewed and accepted `.verified.txt` snapshots
- If dual-mode feature (metadata collection):
  - Implemented in BOTH source-gen AND reflection
  - Tested both modes explicitly
- If performance-critical:
  - Profiled before/after
  - No performance regression
- If using reflection:
  - Tested AOT: `dotnet publish -p:PublishAot=true`
  - Added `[DynamicallyAccessedMembers]` annotations
- No `.received.txt` files staged
- No breaking changes (or major version bump planned)
