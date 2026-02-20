# Troubleshooting

---

## Snapshot Tests Failing

See `CLAUDE.md` for the quick fix workflow.

If changes are unintentional, investigate what modified the source generator output or public API.

---

## Tests Pass Locally, Fail in CI

**Common Causes**:
1. Forgot to commit `.verified.txt` files
2. Line ending differences (CRLF vs LF)
3. Race conditions in parallel tests

**Check**:
```bash
git status | grep verified.txt
git config core.autocrlf
```

---

## Dual-Mode Behavior Differs

Test passes in one mode but fails in the other.

**Check generated code**:
```
obj/Debug/net9.0/generated/TUnit.Core.SourceGenerator/
```

**Common Issues**:
- Attribute not checked in reflection path
- Different data expansion logic
- Missing hook invocation in one mode

---

## AOT Compilation Fails

See `mandatory-rules.md` for annotation patterns.

**Common Causes**:
- Reflection without `[DynamicallyAccessedMembers]`
- Dynamic code generation

---

## Performance Regression

**Diagnose**:
```bash
cd TUnit.Performance.Tests
dotnet run -c Release
```

**Common Causes**:
- Missing reflection cache
- Allocations in hot paths
- Blocking on async

---

## TUnit.TestProject Shows Many Failures

This is expected. Many tests verify failure scenarios.

See `workflows.md`.
