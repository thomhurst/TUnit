# Testing Checklist for Internal Method Fix

## Pre-Merge Testing Required

Since tests could not be run in the development environment (requires .NET 10 SDK), please verify:

### 1. Build Verification
```bash
# Should compile without errors
dotnet build TUnit.Core.SourceGenerator/TUnit.Core.SourceGenerator.csproj -c Release
```

### 2. Source Generator Tests
```bash
# Run the source generator test suite
dotnet test TUnit.Core.SourceGenerator.Tests/TUnit.Core.SourceGenerator.Tests.csproj

# Specifically check the internal method test
dotnet test TUnit.Core.SourceGenerator.Tests/TUnit.Core.SourceGenerator.Tests.csproj --filter "FullyQualifiedName~InternalTestMethodTests"
```

### 3. Snapshot Updates
If the source generator tests pass but show snapshot mismatches, accept the new snapshots:
```bash
# In TUnit.Core.SourceGenerator.Tests directory
# Windows
for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"

# Linux/macOS
for file in *.received.txt; do mv "$file" "${file%.received.txt}.verified.txt"; done
```

Expected snapshot file: `InternalTestMethodTests.Test.verified.txt`

### 4. Integration Test
```bash
# Test the actual scenario from the issue
dotnet test TUnit.TestProject/TUnit.TestProject.csproj --filter "FullyQualifiedName~InternalTestWithArgumentsTest"
```

Expected result: Test should be discovered and pass ✅

### 5. Regression Testing
Run the full test suite to ensure no existing tests are broken:
```bash
# Run all TUnit tests
dotnet test

# Or specifically the public API tests
dotnet test TUnit.PublicAPI/TUnit.PublicAPI.csproj
```

## What to Look For

### Generated Code Inspection
Check the generated file in `TUnit.TestProject/obj/Debug/net*/TUnit.Core.SourceGenerator/`:
- File named like: `InternalTestWithArgumentsTest_TestMethod_*.g.cs`

Should contain:
1. **BindingFlags with NonPublic**:
   ```csharp
   ReflectionInfo = typeof(...).GetMethod(..., 
       BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, ...)
   ```

2. **Activator.CreateInstance for internal class**:
   ```csharp
   InstanceFactory = (typeArgs, args) => 
       global::System.Activator.CreateInstance(typeof(...), true)!
   ```

3. **Reflection-based invoker**:
   ```csharp
   InvokeTypedTest = async (instance, args, cancellationToken) =>
   {
       var methodInfo = typeof(...).GetMethod(..., 
           BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
       ...
       methodInfo.Invoke(instance, methodArgs);
   }
   ```

### Expected Test Output
```
InternalTestWithArgumentsTest.TestMethod("1", "2") - Passed ✅
```

### Common Issues and Solutions

#### Issue: Test not discovered
- **Check**: Is the source generator running? Look for `.g.cs` files in `obj/`
- **Fix**: Clean and rebuild: `dotnet clean && dotnet build`

#### Issue: NullReferenceException during discovery  
- **Check**: Does the generated `ReflectionInfo` include `NonPublic`?
- **Fix**: Verify CodeGenerationHelpers.cs changes are included

#### Issue: Cannot access internal type/method
- **Check**: Are internal members being accessed directly instead of via reflection?
- **Fix**: Verify TestMetadataGenerator.cs and InstanceFactoryGenerator.cs changes

#### Issue: Snapshot test fails
- **Check**: Is the generated code correct but different from snapshot?
- **Fix**: Accept new snapshot if code is correct (see step 3 above)

## Performance Check
Run a quick benchmark to ensure reflection overhead is acceptable:
```bash
# Create a test with many internal test methods
# Run with timing
dotnet test --logger "console;verbosity=detailed"
```

Expected: Minimal difference between public and internal method execution times.

## Additional Verification

### Check Other Data Source Attributes
Verify internal methods work with other data source attributes:
- `[MethodDataSource]`
- `[ClassDataSource]`  
- `[MatrixData]`
- `[PropertyDataSource]`

### Check Async Methods
```csharp
internal async Task TestAsync() { ... }
```

### Check Generic Methods
```csharp
internal Task Test<T>(T value) { ... }
```

## Sign-Off Criteria
- [ ] All source generator tests pass
- [ ] Snapshot tests updated and committed
- [ ] Integration test passes (InternalTestWithArgumentsTest)
- [ ] No regressions in existing tests
- [ ] Generated code inspection confirms expected patterns
- [ ] Public API tests pass (no breaking changes)

Once all criteria are met, the fix is ready to merge! 🚀
