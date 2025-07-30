# AOT Improvements Summary

## Changes Made

### 1. Implemented TupleFactory for AOT-Safe Tuple Creation
- **File**: `TUnit.Core/Helpers/TupleFactory.cs`
- Created a factory pattern that handles tuple creation without reflection for common cases
- Falls back to reflection only for edge cases with proper `RequiresUnreferencedCode` attribute

### 2. Updated PropertyInjector
- **File**: `TUnit.Core/PropertyInjector.cs`
- Replaced direct `Activator.CreateInstance` calls with `TupleFactory.CreateTuple`
- Removed `UnconditionalSuppressMessage` and unnecessary tuple helper methods

### 3. Improved Suppression Justifications
- **Files**: Multiple files in TUnit.Core and TUnit.Engine
- Updated all `UnconditionalSuppressMessage` attributes with detailed justifications
- Added guidance for AOT scenarios in each justification
- Improved readability with multi-line formatting

### 4. Extracted Common Reflection Patterns
- **Files**: `TUnit.Core/Helpers/CastHelper.cs`, `TUnit.Core/Extensions/ReflectionExtensions.cs`
- Created `GetValuePropertySafe` method to centralize property access
- Reduced duplicate pragma suppressions

### 5. Created AOT Compatibility Attributes
- **File**: `TUnit.Core/Attributes/AotCompatibleAttribute.cs`
- Added `AotCompatibleAttribute` to mark AOT-safe code
- Added `RequiresReflectionAttribute` to mark reflection-dependent code
- Provides clear documentation and alternative approaches

### 6. Enhanced F# Async Support
- **File**: `TUnit.Core/AsyncConvert.cs`
- Added `DynamicDependency` attributes to preserve F# types
- Added `RequiresUnreferencedCode` and `RequiresDynamicCode` attributes
- Improved suppressions with detailed explanations

### 7. Created Mode-Specific Test Discovery
- **Files**: New interfaces and implementations in TUnit.Engine/Discovery/
- `ITestDiscovery` - Base interface with AOT support indicator
- `AotTestDiscovery` - Source-generated metadata discovery
- `ReflectionTestDiscovery` - Reflection-based discovery with proper attributes
- `TestDiscoveryFactory` - Smart factory with auto-detection

## Benefits

1. **Better AOT Compatibility**: Reduced reliance on reflection for common scenarios
2. **Clearer Code Paths**: Separated AOT and reflection implementations
3. **Improved Documentation**: All suppressions now have meaningful justifications
4. **Type Safety**: TupleFactory provides compile-time safety for common tuple types
5. **Progressive Enhancement**: Auto-detection allows graceful fallback from AOT to reflection

## Next Steps

1. Extend source generators to cover more scenarios (property injection, hook discovery)
2. Add AOT verification tests to CI/CD pipeline
3. Document AOT vs Reflection mode usage for end users
4. Consider creating more factory patterns for other reflection-heavy operations