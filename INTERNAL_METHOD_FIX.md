# Fix for Internal Test Methods with Arguments Attribute

## Problem
When a test method with `[Arguments]` attribute is marked as `internal`, the source generator generates code that:
1. Cannot instantiate the internal class from the generated `TUnit.Generated` namespace
2. Cannot call the internal method from the generated namespace
3. Fails to get reflection info using only `BindingFlags.Public`

This causes a `NullReferenceException` during test discovery.

## Solution

### 1. Updated BindingFlags in CodeGenerationHelpers.cs (Lines 44, 48)
Changed from:
```csharp
BindingFlags.Public | BindingFlags.Instance
```
To:
```csharp
BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
```

This allows the reflection code to find internal methods.

### 2. Added Reflection-Based Invoker in TestMetadataGenerator.cs
Added a new method `GenerateReflectionBasedInvoker` that uses reflection to invoke internal methods:
```csharp
private static void GenerateReflectionBasedInvoker(...)
{
    // Uses GetMethod with BindingFlags.Public | BindingFlags.NonPublic
    // Uses MethodInfo.Invoke to call the method
}
```

Modified `GenerateConcreteTestInvoker` to check method accessibility and use reflection for non-public methods.

### 3. Updated InstanceFactoryGenerator.cs
Modified `GenerateInstanceFactory` to:
- Check if the type is accessible (public) from the generated namespace
- Use `Activator.CreateInstance(typeof(...), true)` for internal types instead of `new ClassName()`
- Handle both parameterless constructors and constructors with parameters
- Set required properties using reflection for internal types

## Files Changed
1. `TUnit.Core.SourceGenerator/CodeGenerationHelpers.cs` - Added `NonPublic` binding flag
2. `TUnit.Core.SourceGenerator/Generators/TestMetadataGenerator.cs` - Added reflection-based invoker
3. `TUnit.Core.SourceGenerator/CodeGenerators/Helpers/InstanceFactoryGenerator.cs` - Added support for internal types

## Testing
Created test case in `TUnit.TestProject/InternalTestWithArgumentsTest.cs` with:
- Internal class
- Internal test method with `[Arguments]` attribute

## Expected Behavior
- Internal test methods with `[Arguments]` attribute should now be discovered and executed correctly
- Internal test classes should be instantiated properly
- Both reflection and source-generated modes maintain behavioral parity
