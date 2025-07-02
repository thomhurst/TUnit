# Obsolete Code Report for TUnit

This report identifies obsolete code, backward compatibility code, and reflection-related code that needs to be removed or unified in the TUnit codebase.

## 1. Files to Remove Entirely

### Reflection Helper Classes (Not AOT-Compatible)
- `/home/thomh/dev/TUnit/TUnit.Core/Helpers/RobustParameterInfoRetriever.cs` - Uses reflection, marked `[Obsolete]`
- `/home/thomh/dev/TUnit/TUnit.Core/Helpers/MethodInfoRetriever.cs` - Uses reflection, marked `[Obsolete]`
- `/home/thomh/dev/TUnit/TUnit.Core/Helpers/ReflectionToSourceModelHelpers.cs` - Uses reflection, marked `[Obsolete]`

### Backward Compatibility Classes
- `/home/thomh/dev/TUnit/TUnit.Core/BackwardCompatibility.cs` - Contains obsolete `AotFriendlyTestDataSource` and extension methods
- `/home/thomh/dev/TUnit/TUnit.Engine/Services/DataSourceResolver.cs` - Marked `[Obsolete]`, no longer needed
- `/home/thomh/dev/TUnit/TUnit.Engine/Services/IDynamicTestDataResolver.cs` - Interface marked `[Obsolete]`

### Legacy Source Generator Components
- `/home/thomh/dev/TUnit/TUnit.Core/SourceGenerator/TestSourceRegistrar.cs` - Contains legacy registration methods

### Legacy Test Descriptor Interfaces
- `/home/thomh/dev/TUnit/TUnit.Core/DynamicTestMetadata.cs` - Requires reflection, marked with `[RequiresDynamicCode]` and `[RequiresUnreferencedCode]`
- `/home/thomh/dev/TUnit/TUnit.Core/ITestDescriptor.cs` - Legacy interface that DynamicTestMetadata implements
- `/home/thomh/dev/TUnit/TUnit.Core/StaticTestDefinition.cs` - Still uses `[DynamicallyAccessedMembers]` attributes (lines 26-31)

### Test Variation Expansion Interface
- `/home/thomh/dev/TUnit/TUnit.Core/Interfaces/ITestVariationExpander.cs` - Check if this is still needed with the new architecture

## 2. Code to Remove from Existing Files

### `/home/thomh/dev/TUnit/TUnit.Engine/Building/TestBuilder.cs`
- **Lines 58-83**: Remove the fallback `Activator.CreateInstance` code in `CreateInstanceFactory` method
  - Keep only the AOT mode with pre-compiled factory (lines 61-71)
  - Remove the migration period fallback (lines 73-83)
- **Line 58**: Remove the `UnconditionalSuppressMessage` attribute that's only for migration

### `/home/thomh/dev/TUnit/TUnit.Engine/Building/UnifiedTestBuilderPipeline.cs`
- **Lines 144-152**: Remove the obsolete `CreateReflectionPipeline` method entirely

### `/home/thomh/dev/TUnit/TUnit.Core/TestMetadata.cs`
- **Lines 65-66**: Remove the obsolete `DependsOn` property (use `Dependencies` instead)

### `/home/thomh/dev/TUnit/TUnit.Core/Enums/TestExecutionMode.cs`
- Consider removing the `Reflection` enum value if reflection mode is being completely removed
- Update all references to use only `SourceGeneration`

### `/home/thomh/dev/TUnit/TUnit.Engine/CommandLineProviders/ReflectionModeCommandProvider.cs`
- Consider removing this file entirely if reflection mode is being removed
- Or update to show a clear deprecation message

### `/home/thomh/dev/TUnit/TUnit.Core/Services/ModeDetector.cs`
- Remove reflection mode detection logic
- Simplify to always return `TestExecutionMode.SourceGeneration`
- Remove `_isReflectionModeRequested` field and related logic

## 3. TODO Comments to Address

### `/home/thomh/dev/TUnit/TUnit.Core.SourceGenerator/UnifiedTestMetadataGenerator.cs`
- Line 1031: `// TODO: Generate standard hook invokers for user-defined hooks`
- Line 1289: `// TODO: Generate proper data source based on attribute type`
- Line 1369: `// TODO: Add property disposal as an after test hook if any properties implement IAsyncDisposable`

## 4. Classes/Interfaces That Reference Obsolete Code

These files reference obsolete components and need updating:
- Any file importing `IDynamicTestDataResolver`
- Any file using `AotFriendlyTestDataSource`
- Any file using the reflection helper classes
- Any file checking for `TestExecutionMode.Reflection`

## 5. Recommended Actions

1. **Immediate Removals**: Delete all files in section 1
2. **Code Cleanup**: Remove the specific code sections identified in section 2
3. **Update References**: Find and update all references to removed code
4. **Address TODOs**: Implement or remove the TODO items in section 3
5. **Update Tests**: Remove or update any tests that rely on reflection mode
6. **Update Documentation**: Ensure documentation reflects AOT-only mode

## 6. Migration Path

For users still using reflection-based features:
1. Provide clear migration guide to source generation
2. Update error messages to guide users to the new approach
3. Consider a deprecation period with clear warnings before full removal

## 7. Summary of Key Patterns Found

### Obsolete Attributes
- 18 files contain `[Obsolete]` attributes
- Key obsolete classes: `AotFriendlyTestDataSource`, `DataSourceResolver`, `IDynamicTestDataResolver`
- Obsolete methods: `CreateReflectionPipeline`, `RegisterMetadata`

### Reflection Mode Code
- 16 files reference reflection mode
- `TestExecutionMode.Reflection` enum value
- `ReflectionModeCommandProvider` for CLI
- `ModeDetector` with reflection mode detection logic

### Backward Compatibility
- `BackwardCompatibility.cs` with obsolete extension methods
- Migration/fallback code in `TestBuilder.cs`
- Legacy `DependsOn` property in `TestMetadata.cs`

### Reflection Dependencies
- Helper classes using `[RequiresDynamicCode]` and `[RequiresUnreferencedCode]`
- `DynamicTestMetadata` requiring runtime type resolution
- `Activator.CreateInstance` usage in fallback code

### Legacy Interfaces
- `ITestDescriptor` interface hierarchy
- `DynamicTestMetadata` and `StaticTestDefinition` implementations
- Test variation expansion interfaces that may be obsolete