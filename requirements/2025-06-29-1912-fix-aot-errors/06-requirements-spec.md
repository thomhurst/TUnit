# Requirements Specification: Fix AOT Compilation Errors

## Problem Statement

TUnit's source generator creates `DynamicTestDataSource` instances in generated code that require reflection APIs, causing AOT compilation errors. The error occurs in `DataSourceExpander.cs:131` where `_dynamicResolver.ResolveAsync()` triggers IL3050/IL2026 warnings.

## Solution Overview

Implement compile-time data source resolution in the source generator to create `StaticTestDataSource` instances where possible, falling back to `DynamicTestDataSource` with appropriate warnings when necessary. This maintains backward compatibility while maximizing AOT compatibility.

## Functional Requirements

### FR1: Aggressive Compile-Time Resolution
- **Requirement**: Attempt compile-time resolution for all data source types where technically possible
- **Scope**: Static methods, instance methods with parameterless constructors, generic methods, all return types
- **Behavior**: Source generator analyzes method signatures and attempts execution during compilation

### FR2: Graceful Error Handling
- **Requirement**: When compile-time resolution fails, emit compiler warning and fall back to `DynamicTestDataSource`
- **Error Types**: Method not found, execution exceptions, type incompatibility, circular dependencies
- **Behavior**: Maintains backward compatibility while providing developer feedback

### FR3: Automatic Type Conversion
- **Requirement**: Handle type conversions automatically using existing `CastHelper` class
- **Conversions**: Primitive boxing, tuple unwrapping, null handling, nullable types
- **Behavior**: Seamless conversion between method return types and test parameter expectations

### FR4: Performance Protection
- **Requirement**: Implement timeouts for compile-time method execution
- **Constraint**: No caching to maintain test isolation
- **Behavior**: Prevents slow methods from blocking builds while ensuring unique test data instances

### FR5: Generic AsyncDataSourceGeneratorAttribute Support
- **Requirement**: Make strongly-typed generic overloads of `AsyncDataSourceGeneratorAttribute` AOT-compatible
- **Warning Attributes**: Apply `[RequiresDynamicCode]` and `[RequiresUnreferencedCode]` only to base untyped versions
- **Behavior**: Strongly-typed versions work with compile-time resolution

### FR6: Transparent Operation
- **Requirement**: Always attempt static resolution first, fall back automatically
- **Configuration**: No feature flags or user configuration required
- **Behavior**: Existing tests continue working with performance improvements where possible

## Technical Requirements

### TR1: Source Generator Modifications
**File**: `/home/thomh/dev/TUnit/TUnit.Core.SourceGenerator/CodeGenerationHelpers.cs`

**Current Issues**:
- `GenerateMethodDataSource()` (line 456): Always creates `DynamicTestDataSource`
- `GenerateClassDataSource()` (line 495): Always creates `DynamicTestDataSource`  
- `GeneratePropertyDataSource()` (line 508): Always creates `DynamicTestDataSource`
- `GenerateCustomDataSource()` (line 515): Always creates `DynamicTestDataSource`

**Required Changes**:
1. Add compile-time method analysis and execution logic
2. Generate `StaticTestDataSource` with pre-resolved values when possible
3. Generate `DynamicTestDataSource` with compiler warnings when fallback needed
4. Integrate with existing `CastHelper` for type conversions
5. Implement timeout mechanisms for method execution

### TR2: Warning Attribute Application
**File**: `/home/thomh/dev/TUnit/TUnit.Core/Attributes/TestData/AsyncDataSourceGeneratorAttribute.cs`

**Required Changes**:
1. Add `[RequiresDynamicCode]` attribute to base untyped classes
2. Add `[RequiresUnreferencedCode]` attribute to base untyped classes
3. Ensure strongly-typed generic overloads remain AOT-compatible
4. Provide clear warning messages for truly dynamic scenarios

### TR3: Architecture Integration
**Constraint**: Follow clean architecture principles (DRY, SOLID, SRP, KISS)

**Design Decisions**:
- Minimize changes to runtime components (`DataSourceExpander.cs`)
- Maintain separation between source generation and runtime execution
- Reuse existing infrastructure (`CastHelper`, `StaticTestDataSource`)
- Create new components only when necessary for clean design

## Implementation Patterns

### Pattern 1: Compile-Time Method Resolution
```csharp
// Current (creates DynamicTestDataSource):
return $"new global::TUnit.Core.DynamicTestDataSource({isShared}) {{ SourceType = typeof({type}), SourceMemberName = \"{method}\" }}";

// New (attempt StaticTestDataSource):
if (CanResolveAtCompileTime(method, type))
{
    var resolvedData = ResolveMethodAtCompileTime(method, type);
    return $"new global::TUnit.Core.StaticTestDataSource({isShared}) {{ Data = {GenerateDataArray(resolvedData)} }}";
}
else
{
    // Emit compiler warning
    ReportDiagnostic(Warning_FallbackToDynamic, method);
    return $"new global::TUnit.Core.DynamicTestDataSource({isShared}) {{ SourceType = typeof({type}), SourceMemberName = \"{method}\" }}";
}
```

### Pattern 2: Type Conversion Integration
```csharp
// Leverage existing CastHelper for conversions
var convertedData = resolvedData.Select(item => CastHelper.ConvertToObjectArray(item));
```

### Pattern 3: Timeout Implementation
```csharp
// Protect against slow methods
using var cts = new CancellationTokenSource(CompileTimeExecutionTimeout);
var result = await ExecuteMethodWithTimeout(method, cts.Token);
```

## Acceptance Criteria

### AC1: AOT Compilation Success
- **Given**: TUnit project configured for AOT compilation
- **When**: Building tests with various data source attributes
- **Then**: No IL3050/IL2026 warnings in source-generated code paths

### AC2: Backward Compatibility
- **Given**: Existing test project with `MethodDataSourceAttribute`, `ArgumentsAttribute`, `AsyncDataSourceGeneratorAttribute`
- **When**: Building and running tests
- **Then**: All tests continue to work with identical behavior

### AC3: Performance Improvement
- **Given**: Tests using static methods for data sources
- **When**: Running test discovery
- **Then**: Faster discovery due to compile-time resolution (no reflection at runtime)

### AC4: Clear Warnings
- **Given**: Tests using truly dynamic data sources
- **When**: Compiling for AOT
- **Then**: Clear compiler warnings indicating AOT incompatibility with guidance

### AC5: Type Safety
- **Given**: Data source methods returning various types (`IEnumerable<int>`, `IEnumerable<(string, int)>`, etc.)
- **When**: Tests expect `object[]` parameters
- **Then**: Automatic type conversion works correctly

## Assumptions

1. **Build Performance**: Compile-time method execution is acceptable for build performance (mitigated by timeouts)
2. **Side Effects**: Data source methods are generally side-effect free or acceptable to execute during compilation
3. **Dependencies**: Most data source methods don't require complex runtime dependencies unavailable during compilation
4. **Timeout Values**: Reasonable timeout values can be determined for compile-time execution (suggest 5-10 seconds)
5. **Error Reporting**: Compiler warnings are sufficient feedback for fallback scenarios

## Related Features

- **Unified Test Builder Architecture**: Ensures separation between source generation and runtime
- **Existing StaticTestDataSource**: Already handles compile-time data correctly for `ArgumentsAttribute`
- **CastHelper Class**: Provides type conversion infrastructure
- **Microsoft.Testing.Platform Integration**: Must maintain compatibility with platform capabilities