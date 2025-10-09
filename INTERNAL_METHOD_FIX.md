# Fix for Internal Test Methods with Arguments Attribute

## Problem
When a test method with `[Arguments]` attribute is marked as `internal`, the source generator generates code that:
1. Cannot instantiate the internal class from the generated `TUnit.Generated` namespace
2. Cannot call the internal method from the generated namespace  
3. Fails to get reflection info using only `BindingFlags.Public`

This causes a `NullReferenceException` during test discovery.

## Root Cause
The generated code is placed in the `TUnit.Generated` namespace, which is separate from the test class namespace. C# accessibility rules prevent:
- Creating instances of internal classes using `new` from another namespace
- Calling internal methods directly from another namespace
- Getting MethodInfo/ParameterInfo for internal members using only Public binding flags

## Solution

### 1. Updated BindingFlags in CodeGenerationHelpers.cs (Lines 44, 48)
**Changed from:**
```csharp
BindingFlags.Public | BindingFlags.Instance
```

**Changed to:**
```csharp
BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
```

**Why:** This allows the reflection code in the generated `ReflectionInfo` property to find internal methods. Without `NonPublic`, `GetMethod()` returns null for internal methods, causing the NullReferenceException on `.GetParameters()[0]`.

### 2. Added Reflection-Based Invoker in TestMetadataGenerator.cs

**Added new method:**
```csharp
private static void GenerateReflectionBasedInvoker(
    CodeWriter writer, 
    TestMethodMetadata testMethod, 
    string className, 
    string methodName, 
    bool isAsync, 
    bool hasCancellationToken, 
    IParameterSymbol[] parametersFromArgs)
{
    // Gets MethodInfo using NonPublic binding flags
    var methodInfo = typeof(ClassName).GetMethod(
        "MethodName",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    
    // Builds argument array
    var methodArgs = new object?[argCount];
    
    // Invokes using reflection
    var result = methodInfo.Invoke(instance, methodArgs);
}
```

**Modified `GenerateConcreteTestInvoker`:**
```csharp
// Check accessibility
var isMethodAccessible = testMethod.MethodSymbol.DeclaredAccessibility == Accessibility.Public;

// Use reflection for internal methods
if (!isMethodAccessible)
{
    GenerateReflectionBasedInvoker(...);
    return;
}

// Use direct calls for public methods (existing code)
```

**Why:** Cannot call internal methods directly from generated namespace. Must use `MethodInfo.Invoke()` which respects CLR visibility rules.

### 3. Updated InstanceFactoryGenerator.cs

**Added accessibility check:**
```csharp
var isTypeAccessible = typeSymbol is INamedTypeSymbol namedTypeSymbol && 
                      namedTypeSymbol.DeclaredAccessibility == Accessibility.Public;
```

**For internal types:**
```csharp
// Parameterless constructor
InstanceFactory = (typeArgs, args) => 
    global::System.Activator.CreateInstance(typeof(ClassName), true)!;

// Constructor with parameters
var instance = global::System.Activator.CreateInstance(
    typeof(ClassName), 
    true, 
    new object?[] { arg1, arg2, ... })!;
```

**For public types (unchanged):**
```csharp
InstanceFactory = (typeArgs, args) => new ClassName();
```

**Why:** Cannot use `new` operator for internal types from generated namespace. `Activator.CreateInstance` with `nonPublic: true` can create internal types.

## Files Changed
1. `TUnit.Core.SourceGenerator/CodeGenerationHelpers.cs`
   - Lines 44, 48: Added `NonPublic` to binding flags

2. `TUnit.Core.SourceGenerator/Generators/TestMetadataGenerator.cs`
   - Added `GenerateReflectionBasedInvoker` method (~60 lines)
   - Modified `GenerateConcreteTestInvoker` to check accessibility and route to reflection invoker

3. `TUnit.Core.SourceGenerator/CodeGenerators/Helpers/InstanceFactoryGenerator.cs`
   - Modified `GenerateInstanceFactory` to check type accessibility
   - Modified `GenerateTypedConstructorCall` to handle internal types
   - Added `Activator.CreateInstance` path for internal types

## Testing
Created test case in `TUnit.TestProject/InternalTestWithArgumentsTest.cs`:
```csharp
internal sealed class UnitTest
{
    [Test]
    [Arguments("1", "2")]
    internal Task TestMethod(string s1, string s2) => Task.CompletedTask;
}
```

## Generated Code Examples

### Before Fix (Fails)
```csharp
// ReflectionInfo - Returns null for internal method!
ReflectionInfo = typeof(global::Namespace.InternalClass)
    .GetMethod("InternalMethod", BindingFlags.Public | BindingFlags.Instance, ...)!
    .GetParameters()[0]  // NullReferenceException here!

// InstanceFactory - Compilation error!
InstanceFactory = (typeArgs, args) => new global::Namespace.InternalClass();  
// Error: Cannot access internal type

// InvokeTypedTest - Compilation error!
InvokeTypedTest = async (instance, args, cancellationToken) =>
{
    instance.InternalMethod(...);  // Error: Cannot access internal member
};
```

### After Fix (Works)
```csharp
// ReflectionInfo - Finds internal method!
ReflectionInfo = typeof(global::Namespace.InternalClass)
    .GetMethod("InternalMethod", 
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, ...)!
    .GetParameters()[0]

// InstanceFactory - Uses Activator!
InstanceFactory = (typeArgs, args) => 
    global::System.Activator.CreateInstance(
        typeof(global::Namespace.InternalClass), true)!;

// InvokeTypedTest - Uses reflection!
InvokeTypedTest = async (instance, args, cancellationToken) =>
{
    var methodInfo = typeof(global::Namespace.InternalClass).GetMethod(
        "InternalMethod",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    
    var methodArgs = new object?[2];
    methodArgs[0] = TUnit.Core.Helpers.CastHelper.Cast<string>(args[0]);
    methodArgs[1] = TUnit.Core.Helpers.CastHelper.Cast<string>(args[1]);
    
    var result = methodInfo.Invoke(instance, methodArgs);
    if (result is global::System.Threading.Tasks.Task task)
    {
        await task;
    }
};
```

## Design Considerations

### Performance
- **Public methods**: Use direct calls (fast, no reflection)
- **Internal methods**: Use reflection (slightly slower, but necessary)

This is acceptable because:
1. Internal methods are less common than public ones
2. The reflection overhead is minimal compared to test execution
3. Maintaining test discoverability is more important than micro-optimization

### Behavioral Parity
Both source-generated and reflection modes now:
- Support internal test classes
- Support internal test methods  
- Work with `[Arguments]` and other data source attributes
- Produce identical test results

### Alternative Approaches Considered
1. **InternalsVisibleTo**: Would require modifying user projects
2. **Make everything public**: Goes against C# design principles
3. **Block internal methods**: Bad user experience (regression from xUnit)

## Troubleshooting

### If tests still don't discover:
1. Check that the source generator is running: Look for `.g.cs` files in `obj/` directory
2. Verify BindingFlags include `NonPublic` in generated reflection code
3. Check for compilation errors in generated code

### If tests discover but fail to run:
1. Verify the `InvokeTypedTest` delegate uses reflection for internal methods
2. Check that `InstanceFactory` uses `Activator.CreateInstance` for internal types
3. Ensure method signature matches (parameters, return type)

### If nested types fail:
- Nested internal types in public classes require the parent to be public
- This is enforced by the `ClassAccessibilityAnalyzer`
- Only top-level internal classes are supported by this fix

## Related Code
- Reflection mode: `TUnit.Engine/Discovery/ReflectionTestDataCollector.cs`
- Type accessibility: Uses standard .NET reflection which handles internal types
- Already worked for internal methods, as it uses reflection throughout

## Future Enhancements
- Could cache MethodInfo objects to improve reflection performance
- Could generate UnsafeAccessor methods in C# 12+ for better performance
- Could detect and warn about performance implications of internal methods
