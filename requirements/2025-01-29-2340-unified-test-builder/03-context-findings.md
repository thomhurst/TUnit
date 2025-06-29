# Context Findings

## Current Architecture Analysis

### Test Building Flow
1. **Source Generation Phase** (AOT path):
   - `UnifiedTestMetadataGenerator` finds all `[Test]` methods
   - Generates `TestMetadata` instances with pre-compiled invokers
   - Registers metadata in `UnifiedTestMetadataRegistry`
   - Skips generic types (handled by InheritsTestsGenerator)
   - Skips async data source generators (handled separately)

2. **Runtime Phase**:
   - `TestFactory` expands metadata into `ExecutableTest` instances
   - Handles data source resolution (static and dynamic)
   - Manages generic type resolution
   - Creates cartesian products for matrix tests
   - Injects properties after instance creation

### Key Components

#### TestMetadata (Core Data Structure)
- Unified metadata for all tests
- Contains both AOT invokers and reflection info
- Supports data sources at class, method, and property levels
- Has generic type/method information

#### TestFactory (Current Builder)
- Creates `ExecutableTest` from `TestMetadata`
- Has separate paths for AOT and reflection
- Handles data expansion with complex cartesian products
- Manages property injection
- Resolves generic types

#### Data Source System
- `TestDataSource` abstract base class
  - `StaticTestDataSource` - compile-time data
  - `DynamicTestDataSource` - runtime resolution
- `IDataSourceProvider` interface for custom providers
- `DataSourceResolver` handles runtime resolution
- Supports Arguments, MethodDataSource, AsyncDataSourceGenerator

### Issues Identified

1. **Duplication**:
   - AOT and reflection paths in TestFactory are largely duplicated
   - Generic handling logic is complex and scattered
   - Data source resolution happens in multiple places

2. **Complexity**:
   - TestFactory is doing too much (1139 lines)
   - Mixed responsibilities: building, data resolution, generic handling
   - Complex cartesian product generation with nested tracking

3. **Inconsistencies**:
   - Different code paths for AOT vs reflection
   - Generic tests handled differently than regular tests
   - Property injection is an afterthought

### Files That Need Modification

#### Core Files to Refactor:
- `/TUnit.Engine/TestFactory.cs` - main builder to split
- `/TUnit.Core/TestMetadata.cs` - may need adjustments
- `/TUnit.Engine/ExecutableTest.cs` - target output structure

#### Data Source Files:
- `/TUnit.Engine/Services/DataSourceResolver.cs`
- `/TUnit.Core/DataSources/*` - various providers
- `/TUnit.Engine/Interfaces/IDataSourceResolver.cs`

#### Source Generation:
- `/TUnit.Core.SourceGenerator/UnifiedTestMetadataGenerator.cs`
- `/TUnit.Core.SourceGenerator/CodeGenerators/InheritsTestsGenerator.cs`
- Various data provider generators in source generator project

### Similar Features Analyzed

1. **xUnit Approach**:
   - Theory/InlineData for data-driven tests
   - Less flexible than TUnit's approach
   - No property injection

2. **NUnit Approach**:
   - TestCaseSource similar to MethodDataSource
   - Values/ValueSource for parameters
   - No unified metadata structure

### Technical Constraints

1. **AOT Requirements**:
   - No reflection in source generation mode
   - All invokers must be pre-compiled
   - Generic types need special handling

2. **Performance**:
   - Data source resolution has timeouts (30s)
   - Cartesian products have limits (depth, combinations)
   - Need efficient caching for shared data sources

3. **Compatibility**:
   - Must support F#/VB.NET through reflection
   - Need to maintain test discovery protocol
   - Hook system must continue working

### Integration Points

1. **TestDiscoveryService** - consumes executable tests
2. **UnifiedTestExecutor** - runs the tests
3. **Hook system** - lifecycle management
4. **Parallel execution** - scheduling constraints
5. **Test context** - needs test details during execution