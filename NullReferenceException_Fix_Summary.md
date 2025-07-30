# NullReferenceException Fix Summary

## Problem
A `NullReferenceException` was occurring in `TestFilterService.BuildPath()` at line 98 when accessing `test.Context.TestDetails.MethodMetadata.Class` for dynamic tests.

## Root Cause
In `TestBuilderPipeline.cs`, when processing dynamic tests (lines 52-89), the code was creating a `TestContext` but never setting its `TestDetails` property. The `TestContext.TestDetails` property is marked as `required` and must be initialized.

## Solution
Added code to create and set `TestDetails` for dynamic tests, similar to how it's done for failed tests:

```csharp
// Create TestDetails for dynamic tests
var testDetails = new TestDetails
{
    TestId = testId,
    TestName = metadata.TestName,
    ClassType = metadata.TestClassType,
    MethodName = metadata.TestMethodName,
    ClassInstance = null!,
    TestMethodArguments = [],
    TestClassArguments = [],
    TestFilePath = metadata.FilePath ?? "Unknown",
    TestLineNumber = metadata.LineNumber ?? 0,
    TestMethodParameterTypes = metadata.ParameterTypes,
    ReturnType = typeof(Task),
    MethodMetadata = metadata.MethodMetadata,
    Attributes = [],
};

// Set the TestDetails on the context
context.TestDetails = testDetails;
```

## Key Insights
1. The `MethodMetadata` is properly created by `AotTestDataCollector` using `MetadataBuilder.CreateMethodMetadata()` (line 222)
2. All required fields including `MethodMetadata.Class` are populated during metadata creation
3. The issue was simply that the `TestDetails` object wasn't being created and assigned to the context for dynamic tests
4. Regular tests created by `TestBuilder` already had proper `TestDetails` initialization

## Result
- Dynamic tests now have properly initialized `TestContext` with `TestDetails`
- `TestFilterService.BuildPath()` can safely access `test.Context.TestDetails.MethodMetadata.Class`
- No more `NullReferenceException` when filtering dynamic tests
- Both TUnit.Core and TUnit.Engine build successfully without errors