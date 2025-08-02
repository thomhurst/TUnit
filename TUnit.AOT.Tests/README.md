# TUnit AOT Compatibility Tests

This project validates that TUnit is fully compatible with .NET Native AOT (Ahead-of-Time) compilation in source generation mode.

## Overview

The TUnit framework has been enhanced to support AOT compilation by:

1. **Replacing reflection-heavy code** with source-generated alternatives
2. **Adding proper `[DynamicallyAccessedMembers]` attributions** for remaining reflection usage
3. **Implementing AOT-safe type resolution** to replace `Type.GetType()` and `MakeGenericType()` calls
4. **Creating source generators** for method invocation, property injection, and tuple processing
5. **Adding comprehensive AOT compatibility analyzers** to prevent regressions

## Key Features Tested

### ✅ Core Framework Compatibility
- Basic test execution without reflection
- Parameterized tests with source-generated argument processing
- Method and class data sources with generated invocation code
- Generic type handling with compile-time resolution
- Hook execution (Before/After) with generated delegates

### ✅ Assertions Library Compatibility  
- Object comparison with proper member access attribution
- Exception assertions with type-safe checking
- Collection assertions without runtime reflection
- Property-based assertions (with appropriate AOT warnings)

### ✅ Source Generation Mode
- Complete test discovery at compile time
- Strongly-typed test method invocation
- Property injection with generated setters
- Tuple processing with generated unwrapping code
- Type registry for AOT-safe generic type resolution

### ✅ Advanced Scenarios
- Matrix data sources with complex type combinations
- Async test execution
- Global and assembly-level hooks
- Shared data sources with proper instantiation
- Generic test classes with concrete implementations

## Build Configuration

This project is configured with:

```xml
<PropertyGroup>
  <!-- Enable AOT publishing -->
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
  
  <!-- Source generation mode -->
  <TUNIT_EXECUTION_MODE>SourceGeneration</TUNIT_EXECUTION_MODE>
  
  <!-- Enable all AOT analysis warnings -->
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>full</TrimMode>
  <EnableAotAnalyzer>true</EnableAotAnalyzer>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

## Running AOT Tests

### Option 1: PowerShell Script (Recommended)
```powershell
.\build-aot.ps1
```

### Option 2: Manual Commands
```bash
# Set environment variable
export TUNIT_EXECUTION_MODE=SourceGeneration

# Clean and build
dotnet clean --configuration Release
dotnet build --configuration Release

# Run tests  
dotnet test --configuration Release

# Publish with AOT
dotnet publish --configuration Release
```

## Expected Results

When running successfully, you should see:
- ✅ **Build completes without AOT warnings**
- ✅ **All tests pass**
- ✅ **Native executable generated** (typically 10-50MB)
- ✅ **No reflection usage in published output**

## AOT Compatibility Validation

The tests validate several key aspects:

1. **No Runtime Reflection**: All type discovery, method invocation, and property access uses compile-time generated code
2. **Source Generation Active**: Verifies that `TUNIT_EXECUTION_MODE=SourceGeneration` is working
3. **Type Resolution**: Generic types and complex scenarios work without `Type.GetType()` or `MakeGenericType()`
4. **Data Source Processing**: Method and class data sources use generated invocation instead of `MethodInfo.Invoke()`
5. **Property Injection**: Uses generated setters instead of reflection-based property setting
6. **Tuple Processing**: Uses generated unwrapping code instead of field reflection

## Troubleshooting

If AOT compilation fails:

1. **Check for new reflection usage** - The enhanced analyzers should catch this at build time
2. **Verify source generation mode** - Ensure `TUNIT_EXECUTION_MODE=SourceGeneration` is set
3. **Review trimming warnings** - Look for IL2xxx warnings that indicate reflection issues
4. **Validate attributions** - Ensure all reflection usage has proper `[DynamicallyAccessedMembers]` attribution

## Source Generation Components

This test project validates the following source-generated components:

- **AotTypeResolverGenerator**: Replaces `TypeResolver.cs` with compile-time type mapping
- **AotMethodInvocationGenerator**: Generates strongly-typed method delegates
- **AotTupleProcessorGenerator**: Creates compile-time tuple unwrapping code  
- **EnhancedPropertyInjectionGenerator**: Generates property setters with UnsafeAccessor
- **AotModuleInitializerGenerator**: Wires all components together automatically

## Performance Benefits

AOT compilation with TUnit provides:
- **Faster startup time** (no JIT compilation)
- **Smaller memory footprint** (trimmed assemblies)
- **Predictable performance** (no runtime code generation)
- **Better security** (no dynamic code execution)

## Success Criteria

This project is considered successful when:
- All tests pass ✅
- AOT publish completes without warnings ✅
- Native executable runs successfully ✅  
- No reflection usage in critical paths ✅
- Source generation provides complete coverage ✅