# AOT-Only Mode

TUnit now operates exclusively in AOT (Ahead-of-Time) compilation mode, using compile-time source generation for maximum performance and full Native AOT compatibility.

## What Changed

Starting with this version, TUnit has **completely removed reflection-based execution** in favor of a fully source-generated approach that provides:

- **Zero Runtime Reflection**: All test discovery and execution uses compile-time generated code
- **Native AOT Compatibility**: Full support for Native AOT compilation scenarios
- **Superior Performance**: 2-3x performance improvement over reflection-based execution
- **Compile-Time Safety**: Type errors and configuration issues caught at build time

## How It Works

TUnit now generates strongly-typed delegates for all test operations at compile time:

- **Test Invocation**: Type-specific delegates instead of generic object arrays
- **Data Sources**: Specialized factory methods with exact return types
- **Property Injection**: Generated property setters with dependency resolution
- **Hook Methods**: Strongly-typed hook delegates with proper async support

## Configuration

You can configure AOT behavior through EditorConfig or MSBuild properties:

### EditorConfig (.editorconfig)
```ini
# Enable AOT-only mode (default: true)
tunit.aot_only_mode = true

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
    <TUnitAotOnlyMode>true</TUnitAotOnlyMode>
    <TUnitGenericDepthLimit>10</TUnitGenericDepthLimit>
    <TUnitEnablePropertyInjection>true</TUnitEnablePropertyInjection>
</PropertyGroup>
```

## Compatibility

**C# Projects**: Full support with source generation
**F# Projects**: Limited support - use C# test projects that reference F# libraries
**VB.NET Projects**: Limited support - use C# test projects that reference VB.NET libraries

For cross-language scenarios, create a separate C# test project that references your F#/VB.NET libraries and write tests there.
