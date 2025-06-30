# Context Findings

## Current Architecture Analysis

### Problem Identified
The TUnit source generator is creating `DynamicTestDataSource` instances in generated code, which require reflection APIs that are incompatible with AOT compilation. The AOT errors occur in `/home/thomh/dev/TUnit/TUnit.Engine/Building/Expanders/DataSourceExpander.cs` at line 131.

### Current Data Source Architecture
1. **TestDataSource (Base)**: Abstract base class for all data sources
2. **StaticTestDataSource**: For compile-time known values (AOT-compatible)
3. **DynamicTestDataSource**: For runtime resolution via reflection (NOT AOT-compatible)

### Source Generator Issues
In `CodeGenerationHelpers.cs`, the generator creates `DynamicTestDataSource` for:
- Method data sources (line 456)
- Class data sources (line 495) 
- Property data sources (line 508)
- Custom data attributes (line 515)

### Key Files Requiring Analysis
- `/home/thomh/dev/TUnit/TUnit.Core.SourceGenerator/CodeGenerationHelpers.cs` - Data source generation
- `/home/thomh/dev/TUnit/TUnit.Core.SourceGenerator/UnifiedTestMetadataGenerator.cs` - Main generator
- `/home/thomh/dev/TUnit/TUnit.Core/TestDataSources.cs` - Data source definitions
- `/home/thomh/dev/TUnit/TUnit.Engine/Building/Expanders/DataSourceExpander.cs` - Runtime resolution

### AOT Compatibility Requirements
- Source generators should only emit static data structures
- Runtime reflection usage must be avoided in AOT scenarios
- Need to pre-resolve data at compile time where possible
- Dynamic resolution should only be used in reflection mode

### Current Unified Architecture
The project recently implemented a unified test builder architecture with:
- AOT mode with pre-compiled factories/invokers
- Reflection mode as fallback
- Proper separation between source generation and runtime

## Detailed Analysis Results

### Generated Code Structure
From `MethodDataSourceDrivenTests.Test.verified.txt`, the source generator creates:
- `MethodMetadata` with attribute metadata for each test method
- `AttributeMetadata` instances that recreate the original attributes
- No direct `TestDataSource` creation in the main metadata generation

### Data Source Generation Locations
In `CodeGenerationHelpers.cs`, data sources are created in these methods:
- `GenerateInlineDataDataSource()` - **Already correct**: Creates `StaticTestDataSource` for `ArgumentsAttribute`
- `GenerateMethodDataSource()` - **Problem**: Creates `DynamicTestDataSource` for `MethodDataSourceAttribute`
- `GenerateClassDataSource()` - **Problem**: Creates `DynamicTestDataSource` for class-level data
- `GeneratePropertyDataSource()` - **Problem**: Creates `DynamicTestDataSource` for property data
- `GenerateCustomDataSource()` - **Problem**: Creates `DynamicTestDataSource` for custom attributes

### Attribute Analysis

#### ArgumentsAttribute (Already AOT-Compatible)
- Simple attribute with compile-time `object?[]` values
- Currently generates `StaticTestDataSource` correctly
- No changes needed

#### MethodDataSourceAttribute (Needs Fix)
- Has `[DynamicallyAccessedMembers]` attributes for trim warnings
- References methods by string name for flexibility
- **Can be made AOT-compatible**: Most cases reference static methods with known return types
- Need to evaluate methods at compile time where possible

#### AsyncDataSourceGeneratorAttribute (Inherently Dynamic)
- Abstract base requiring runtime implementation
- Generates async data sources dynamically
- **Cannot be made fully AOT-compatible**: Requires `[RequiresDynamicCode]` attribute
- Used for truly dynamic scenarios (database queries, API calls, etc.)

### Current Problems

#### In CodeGenerationHelpers.cs:456
```csharp
return $"new global::TUnit.Core.DynamicTestDataSource({isShared.ToString().ToLowerInvariant()}) 
{{ 
    SourceType = typeof({containingType.GloballyQualified()}), 
    SourceMemberName = \"{methodName}\" 
}}";
```
**Issue**: Always creates `DynamicTestDataSource` even for static methods that could be resolved at compile time.

#### In DataSourceExpander.cs:131
```csharp
return await _dynamicResolver.ResolveAsync(dynamicSource, level);
```
**Issue**: Runtime reflection resolution triggers AOT warnings IL3050/IL2026.

### Solution Approach

#### For Static Method Data Sources
- Analyze method signature at compile time
- If method is static, parameterless, and returns known types, resolve at compile time
- Generate `StaticTestDataSource` with pre-evaluated values
- Fall back to `DynamicTestDataSource` for complex cases

#### For Dynamic Data Sources
- Add `[RequiresDynamicCode]` to `AsyncDataSourceGeneratorAttribute`
- Add `[RequiresUnreferencedCode]` to truly dynamic scenarios
- Provide clear warnings to test authors

#### Backward Compatibility
- All existing test attributes remain unchanged
- Existing tests continue to work
- Performance improves for static cases
- Clear warnings for dynamic cases

## Implementation Requirements

1. **Modify CodeGenerationHelpers.cs**:
   - Add compile-time evaluation logic for static methods
   - Generate `StaticTestDataSource` when possible
   - Keep `DynamicTestDataSource` for truly dynamic cases

2. **Add Warning Attributes**:
   - `[RequiresDynamicCode]` on `AsyncDataSourceGeneratorAttribute`
   - Appropriate warnings for reflection-dependent scenarios

3. **Maintain Compatibility**:
   - No breaking changes to public APIs
   - All existing tests continue working
   - Gradual improvement path for performance