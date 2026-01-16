# Generic Method Discovery with [GenerateGenericTest] + [MethodDataSource]

**Issue:** [#4440](https://github.com/thomhurst/TUnit/issues/4440)
**Date:** 2026-01-16
**Status:** Design Complete

## Problem Statement

When a generic **method** (not class) has both `[GenerateGenericTest]` and `[MethodDataSource]` attributes, tests fail to be discovered at runtime in reflection mode, though the source generator produces correct metadata.

```csharp
public class NonGenericClassWithGenericMethodAndDataSource
{
    [Test]
    [GenerateGenericTest(typeof(int))]
    [GenerateGenericTest(typeof(double))]
    [MethodDataSource(nameof(GetStrings))]
    public async Task GenericMethod_With_DataSource<T>(string input) { }
}
```

**Expected:** 4 tests (int×"hello", int×"world", double×"hello", double×"world")
**Actual:** 0 tests discovered in reflection mode

## Root Cause Analysis

| Mode | Class-level `[GenerateGenericTest]` | Method-level `[GenerateGenericTest]` |
|------|-------------------------------------|--------------------------------------|
| Source Generator | Handled (lines 3680-3733) | Handled (lines 3735-3751) |
| Reflection | Handled (lines 588-625) | **NOT handled** |

In `ReflectionTestDataCollector.cs`, lines 588 and 716 only check for `[GenerateGenericTest]` on classes:
```csharp
var generateGenericTestAttributes = genericTypeDefinition.GetCustomAttributes<GenerateGenericTestAttribute>(inherit: false).ToArray();
```

No equivalent code exists for method-level `[GenerateGenericTest]`.

## Solution Design

### Architecture Overview

Refactor discovery to separate concerns into three distinct responsibilities:

```
DiscoverTestsAsync(assembly)
  └─> for each type:
        └─> DiscoverTestsFromTypeAsync(type)
              └─> for each (concreteClass, classData) in ResolveClassInstantiations(type):
                    └─> for each method in GetTestMethods(concreteClass):
                          └─> for each concreteMethod in ResolveMethodInstantiations(method):
                                └─> BuildTestMetadata(concreteClass, concreteMethod, classData)
```

| Method | Responsibility |
|--------|----------------|
| `ResolveClassInstantiationsAsync` | Yields `(Type, object[]?)` for each concrete class variant |
| `ResolveMethodInstantiations` | Yields `MethodInfo` for each concrete method variant |
| `BuildTestMetadata` | Creates metadata from concrete class + concrete method (unchanged) |

### Method 1: `ResolveClassInstantiationsAsync`

**Signature:**
```csharp
private static async IAsyncEnumerable<(Type ConcreteType, object?[]? ClassData)> ResolveClassInstantiationsAsync(
    [DynamicallyAccessedMembers(...)] Type type,
    [EnumeratorCancellation] CancellationToken cancellationToken)
```

**Logic:**
1. If type is NOT a generic type definition:
   - Yield `(type, null)` once

2. If type IS a generic type definition:
   - Check for `[GenerateGenericTest]` attributes on class
     - For each attribute: extract type args, validate constraints, yield `(concreteType, null)`
   - Check for class-level data sources
     - For each data row: infer type args, yield `(concreteType, dataRow)`
   - If neither found: yield nothing (can't resolve open generic)

### Method 2: `ResolveMethodInstantiations`

**Signature:**
```csharp
private static IEnumerable<MethodInfo> ResolveMethodInstantiations(
    Type concreteClassType,
    MethodInfo method)
```

**Note:** Synchronous because `[GenerateGenericTest]` attributes are available immediately.

**Logic:**
1. If method is NOT a generic method definition:
   - Yield `method` once

2. If method IS a generic method definition:
   - Get `[GenerateGenericTest]` attributes from method
   - If attributes found:
     - For each: extract type args, validate constraints, yield `method.MakeGenericMethod(typeArgs)`
   - If no attributes:
     - Yield method as-is (TestBuilder will attempt inference from data sources)

### Error Handling

All errors become **visible failed tests** in the test explorer:

```csharp
private static TestMetadata CreateFailedDiscoveryTest(
    Type? testClass,
    MethodInfo? testMethod,
    string errorMessage,
    Exception? exception = null)
{
    var fullMessage = exception != null
        ? $"{errorMessage}\n\nException: {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}"
        : errorMessage;

    return new FailedTestMetadata
    {
        TestName = testMethod?.Name ?? testClass?.Name ?? "Unknown",
        TestClassType = testClass ?? typeof(object),
        TestMethodName = testMethod?.Name ?? "DiscoveryError",
        FailureReason = fullMessage,
        DiscoveryException = exception ?? new TestDiscoveryException(errorMessage)
    };
}
```

**Error scenarios that create failed tests:**
- Constraint violations when calling `MakeGenericType`/`MakeGenericMethod`
- Type argument count mismatch
- Data source retrieval failures
- Reflection failures

**What users see:**
```
❌ GenericMethod_With_DataSource (Discovery Failed)

   [GenerateGenericTest] provides 1 type argument(s) but method
   'GenericMethod_With_DataSource' requires 2.
   Provided: [Int32]
```

### TestBuilder Integration

No changes needed to TestBuilder. By the time metadata reaches TestBuilder:
- Class type is concrete
- Method is concrete (with `GenericMethodTypeArguments` populated)
- `[MethodDataSource]` is preserved on the concrete method

TestBuilder's existing data source handling works unchanged.

## Implementation Plan

### Step 1: Add Resolution Methods

**File:** `TUnit.Engine/Discovery/ReflectionTestDataCollector.cs`

Add two new methods:
- `ResolveClassInstantiationsAsync` - extracts and refactors existing logic from `DiscoverTestsFromGenericTypeAsync`
- `ResolveMethodInstantiations` - new logic for method-level `[GenerateGenericTest]`

### Step 2: Add Error Handling Infrastructure

**File:** `TUnit.Engine/Discovery/ReflectionTestDataCollector.cs`

Add helper method:
- `CreateFailedDiscoveryTest` - creates visible failed test metadata for errors

### Step 3: Refactor Main Discovery Loop

**File:** `TUnit.Engine/Discovery/ReflectionTestDataCollector.cs`

Refactor `DiscoverTestsFromTypeAsync` to use the new resolution methods:
```csharp
await foreach (var (concreteClass, classData) in ResolveClassInstantiationsAsync(type, cancellationToken))
{
    foreach (var method in GetTestMethods(concreteClass))
    {
        foreach (var concreteMethod in ResolveMethodInstantiations(concreteClass, method))
        {
            yield return await BuildTestMetadata(concreteClass, concreteMethod, classData);
        }
    }
}
```

### Step 4: Add Test Fixtures

**File:** `TUnit.TestProject/Bugs/4440/GenericMethodDiscoveryTests.cs`

Create test fixtures covering:
- Non-generic class + generic method + data source (original bug)
- Generic class + generic method + data source (cartesian product)
- Error cases (constraint violations, type arg mismatches)

### Step 5: Add Unit Tests

**File:** `TUnit.Engine.Tests/ResolveClassInstantiationsTests.cs`
**File:** `TUnit.Engine.Tests/ResolveMethodInstantiationsTests.cs`

Unit tests for the new resolution methods in isolation.

### Step 6: Expand Integration Tests

**File:** `TUnit.Engine.Tests/GenericMethodWithDataSourceTests.cs`

Expand existing tests to run in both modes and verify parity.

## Test Matrix

| Scenario | Expected Tests | Source Gen | Reflection |
|----------|---------------|------------|------------|
| Non-generic class, generic method, 2 types, 2 data | 4 | Must pass | Must pass |
| Generic class (2 types), non-generic method, 3 data | 6 | Must pass | Must pass |
| Generic class (2 types), generic method (2 types), 2 data | 8 | Must pass | Must pass |
| Constraint violation | 1 failed | Must show error | Must show error |
| Type arg count mismatch | 1 failed | Must show error | Must show error |

## Files Changed

| File | Change |
|------|--------|
| `TUnit.Engine/Discovery/ReflectionTestDataCollector.cs` | Add resolution methods, refactor discovery loop |
| `TUnit.TestProject/Bugs/4440/GenericMethodDiscoveryTests.cs` | New test fixtures |
| `TUnit.Engine.Tests/GenericMethodWithDataSourceTests.cs` | Expand integration tests |
| `TUnit.Engine.Tests/ResolveClassInstantiationsTests.cs` | New unit tests |
| `TUnit.Engine.Tests/ResolveMethodInstantiationsTests.cs` | New unit tests |

## Success Criteria

1. Issue #4440 scenario discovers 4 tests in both modes
2. Cartesian product (class + method generics) works correctly
3. All errors visible as failed tests in test explorer
4. Source generator and reflection modes produce identical test counts
5. No regression in existing generic class tests
6. All existing tests continue to pass
