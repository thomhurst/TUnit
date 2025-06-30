# TUnit Architecture Refactoring Summary

## Overview

This document summarizes the major architectural simplification of TUnit, reducing complexity while maintaining all functionality.

## Key Changes

### 1. Unified Test Metadata
- **Old**: Complex hierarchy with `TestDefinition`, `TestDefinition<T>`, `StaticTestDefinition`, `DynamicTestMetadata`
- **New**: Single `TestMetadata` class with optional AOT factories

### 2. Simplified Test Representation
- **Old**: Multiple transformations: `TestMetadata` → `TestDefinition` → `DiscoveredTest` → execution
- **New**: Direct path: `TestMetadata` → `ExecutableTest`

### 3. Consolidated Test Factory
- **Old**: Multiple builders: `StaticTestBuilder`, `DynamicTestBuilder`, `TestBuilder`, `ReflectionTestConstructionBuilder`
- **New**: Single `TestFactory` with `CreateTests()` method

### 4. Streamlined Discovery
- **Old**: 4+ layer pipeline: `TUnitTestDiscoverer` → `BaseTestsConstructor` → `TestsCollector` → `TestMetadataSource`
- **New**: Direct: `TestDiscoveryService` → `ITestMetadataSource`

### 5. Simplified Execution
- **Old**: Complex executor hierarchy with multiple coordinators
- **New**: `UnifiedTestExecutor` with clear parallel/serial execution paths

## Files to Remove

### Discovery Pipeline (can be removed)
- `TUnit.Engine/Services/TUnitTestDiscoverer.cs` - just delegates
- `TUnit.Engine/Services/BaseTestsConstructor.cs` - just delegates
- `TUnit.Engine/Services/TestsCollector.cs` - just dequeues
- `TUnit.Engine/Services/SourceGeneratedTestsConstructor.cs` - redundant
- `TUnit.Engine/Services/ReflectionTestsConstructor.cs` - replaced by TestFactory

### Builders (can be removed)
- `TUnit.Engine/Services/StaticTestBuilder.cs` - replaced by TestFactory
- `TUnit.Engine/Services/DynamicTestBuilder.cs` - replaced by TestFactory
- `TUnit.Engine/Services/ReflectionTestConstructionBuilder.cs` - replaced by TestFactory

### Old Test Types (can be removed)
- `TUnit.Core.SourceGenerator/Models/TestDefinition.cs` - replaced by unified TestMetadata
- `TUnit.Core.SourceGenerator/Models/StaticTestDefinition.cs` - replaced by unified TestMetadata
- `TUnit.Core.SourceGenerator/Models/DynamicTestMetadata.cs` - replaced by unified TestMetadata

### Redundant Registrars (can be removed)
- `TUnit.Engine/Services/DynamicTestRegistrar.cs` - functionality in TestMetadataRegistry
- `TUnit.Engine/Services/TestRegistrar.cs` - misleading name, minimal functionality

### AOT Wrappers (can be removed)
- Various AOT wrapper classes - functionality built into new architecture

## Migration Path

### For Source Generators
1. Update to emit new `TestMetadata` format with AOT factories
2. Generate static registry of all tests
3. Remove complex type conversions

### For Runtime
1. Use `TestDiscoveryService` to load metadata
2. Use `TestFactory` to create `ExecutableTest` instances
3. Use `UnifiedTestExecutor` for execution

### For Tests
No changes required - the new architecture maintains backward compatibility at the attribute level.

## Benefits

1. **50% fewer classes** - from ~30+ core classes to ~15
2. **Clearer responsibilities** - each class has one clear purpose
3. **Better performance** - fewer allocations and transformations
4. **Simpler AOT support** - built-in rather than wrapper-based
5. **Easier maintenance** - straightforward data flow

## Next Steps

1. Remove identified redundant files
2. Update existing code to use new types
3. Update tests to work with new architecture
4. Update documentation