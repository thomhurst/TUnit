# NUnit ExpectedResult Migration Code Fixer

**Issue**: #4167
**Date**: 2025-12-25
**Status**: Implemented

## Overview

A Roslyn analyzer + code fixer that extends TUnit's existing NUnit migration infrastructure to handle `ExpectedResult` patterns. Converts NUnit's return-value-based test assertions to TUnit's explicit assertion approach.

## Scope

### Patterns Handled

1. `[TestCase(..., ExpectedResult = X)]` - Inline expected result property
2. `TestCaseData.Returns(X)` - Fluent expected result in data sources

### Patterns Flagged for Manual Review

- Mixed attributes (some with ExpectedResult, some without)
- `TestCaseData` with `.SetName()`, `.SetCategory()`, or other chained methods
- Dynamic/computed `TestCaseData` (loops, conditionals)
- `TestCaseData` constructed outside simple array/collection initializers

## Transformation Examples

### TestCase with ExpectedResult

```csharp
// BEFORE (NUnit)
[TestCase(2, 3, ExpectedResult = 5)]
[TestCase(10, 5, ExpectedResult = 15)]
public int Add(int a, int b) => a + b;

// AFTER (TUnit)
[Test]
[Arguments(2, 3, 5)]
[Arguments(10, 5, 15)]
public async Task Add(int a, int b, int expected)
{
    await Assert.That(a + b).IsEqualTo(expected);
}
```

### TestCaseData.Returns()

```csharp
// BEFORE (NUnit)
public static IEnumerable<TestCaseData> AddCases => new[]
{
    new TestCaseData(2, 3).Returns(5),
    new TestCaseData(10, 5).Returns(15)
};

[TestCaseSource(nameof(AddCases))]
public int Add(int a, int b) => a + b;

// AFTER (TUnit)
public static IEnumerable<(int, int, int)> AddCases => new[]
{
    (2, 3, 5),
    (10, 5, 15)
};

[Test]
[MethodDataSource(nameof(AddCases))]
public async Task Add(int a, int b, int expected)
{
    await Assert.That(a + b).IsEqualTo(expected);
}
```

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Multiple ExpectedResults | Add as method parameter | Preserves parameterized structure, single method |
| Expression-bodied members | Convert to block body | Semantic change warrants explicit block |
| Multiple return statements | Extract to local variable | Cleaner code, single assertion point |
| Null expected values | Use `IsEqualTo(expected)` | Parameter value unknown at compile-time |
| Parameter naming | `expected` | Concise, idiomatic in testing |

## Method Body Transformation

### Case A: Expression-Bodied

```csharp
// BEFORE
public int Add(int a, int b) => a + b;

// AFTER
public async Task Add(int a, int b, int expected)
{
    await Assert.That(a + b).IsEqualTo(expected);
}
```

### Case B: Single Return

```csharp
// BEFORE
public int Add(int a, int b)
{
    var sum = a + b;
    return sum;
}

// AFTER
public async Task Add(int a, int b, int expected)
{
    var sum = a + b;
    await Assert.That(sum).IsEqualTo(expected);
}
```

### Case C: Multiple Returns

```csharp
// BEFORE
public int Factorial(int n)
{
    if (n < 0) return 0;
    if (n <= 1) return 1;
    return n * Factorial(n - 1);
}

// AFTER
public async Task Factorial(int n, int expected)
{
    int result;
    if (n < 0) result = 0;
    else if (n <= 1) result = 1;
    else result = n * Factorial(n - 1);
    await Assert.That(result).IsEqualTo(expected);
}
```

**Algorithm for multiple returns**:
1. Declare `{returnType} result;` at method start
2. Replace each `return X;` with `result = X;`
3. Convert `if (...) return` chains to `if/else if/else`
4. Append `await Assert.That(result).IsEqualTo(expected);` at end

## Implementation

### Analyzer

Extend `NUnitMigrationAnalyzer` to detect:
- `ExpectedResult` named argument in `[TestCase]` attributes
- `.Returns()` method calls on `TestCaseData`

**Diagnostic**: `TUnit0050` (Info severity)
**Message**: "NUnit ExpectedResult can be converted to TUnit assertion"

### Code Fixer

#### New Files

```
TUnit.Analyzers.CodeFixers/
├── NUnitExpectedResultRewriter.cs    # TestCase ExpectedResult transform
└── NUnitTestCaseDataRewriter.cs      # TestCaseData.Returns() transform
```

#### Integration

Add to `NUnitMigrationCodeFixProvider.cs` transformation pipeline:

```csharp
// Before attribute conversion:
root = new NUnitExpectedResultRewriter(semanticModel).Visit(root);
root = new NUnitTestCaseDataRewriter(semanticModel).Visit(root);
```

### TestCase Transformation Steps

1. Extract `ExpectedResult` values from each `[TestCase]`
2. Convert `[TestCase(args, ExpectedResult = X)]` → `[Arguments(args, X)]`
3. Add `expected` parameter with original return type
4. Change return type to `async Task`
5. Transform method body per cases above
6. Add `[Test]` attribute if not present

### TestCaseData Transformation Steps

1. Locate data source method/property
2. For each `TestCaseData`: extract args + `.Returns()` value → tuple
3. Update return type: `IEnumerable<TestCaseData>` → `IEnumerable<(T1, T2, ..., TExpected)>`
4. Transform test method same as TestCase

## Testing

Add to `TUnit.Analyzers.Tests/NUnitMigrationAnalyzerTests.cs`:

- Simple ExpectedResult (single TestCase)
- Multiple TestCase attributes with different expected values
- Expression-bodied methods
- Block-bodied with single return
- Block-bodied with multiple returns
- Null expected values
- Reference type expected values
- TestCaseData.Returns() simple case
- TestCaseData.Returns() with multiple entries
- Mixed scenarios (flagged for manual review)

## Edge Cases

| Scenario | Handling |
|----------|----------|
| Null ExpectedResult | `IsEqualTo(expected)` handles null at runtime |
| Constant references (`int.MaxValue`) | Passed through as parameter value |
| Generic return types | Parameter type matches original return type |
| Nullable return types | Parameter type includes nullability |
| Recursive methods | Works - method signature change is compatible |
| Mixed TestCase (with/without ExpectedResult) | Flag for manual review |
