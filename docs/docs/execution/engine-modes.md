# Engine Modes

TUnit supports both reflection-based and AOT (Ahead-of-Time) compilation modes, providing flexibility for different deployment scenarios.

## Execution Modes

TUnit can run in two modes:

- **Reflection Mode** (Default): Standard execution using runtime reflection for test discovery and execution
- **AOT Mode**: When published with Native AOT, TUnit uses compile-time source generation for full AOT compatibility

## AOT Support

When publishing your tests with Native AOT, TUnit provides:

- **Source-Generated Execution**: All test discovery and execution uses compile-time generated code
- **Native AOT Compatibility**: Full support for Native AOT compilation scenarios  
- **Superior Performance**: 2-3x performance improvement over reflection-based execution
- **Compile-Time Safety**: Type errors and configuration issues caught at build time

## How AOT Mode Works

When using Native AOT publishing, TUnit generates strongly-typed delegates for all test operations at compile time:

- **Test Invocation**: Type-specific delegates instead of generic object arrays
- **Data Sources**: Specialized factory methods with exact return types
- **Property Injection**: Generated property setters with dependency resolution
- **Hook Methods**: Strongly-typed hook delegates with proper async support

## Publishing with Native AOT

To use TUnit with Native AOT:

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
</PropertyGroup>
```

Then publish your project:
```bash
dotnet publish -c Release
```

## Configuration

You can configure TUnit behavior through EditorConfig or MSBuild properties:

### EditorConfig (.editorconfig)
```ini
# Enable AOT optimizations when available (default: true)
tunit.enable_aot_optimizations = true

# Generic type resolution depth (default: 5)
tunit.generic_depth_limit = 5

# Property injection support (default: true)
tunit.enable_property_injection = true

# ValueTask hook support (default: true)
tunit.enable_valuetask_hooks = true

# Verbose diagnostics (default: false)
tunit.enable_verbose_diagnostics = true
```

### MSBuild Properties
```xml
<PropertyGroup>
    <TUnitEnableAotOptimizations>true</TUnitEnableAotOptimizations>
    <TUnitGenericDepthLimit>10</TUnitGenericDepthLimit>
    <TUnitEnablePropertyInjection>true</TUnitEnablePropertyInjection>
</PropertyGroup>
```

## Compatibility

**C# Projects**: Full support with source generation
**F# Projects**: Limited support - use C# test projects that reference F# libraries
**VB.NET Projects**: Limited support - use C# test projects that reference VB.NET libraries

For cross-language scenarios, create a separate C# test project that references your F#/VB.NET libraries and write tests there.
