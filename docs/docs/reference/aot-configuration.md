# AOT Configuration Reference

TUnit's AOT-only mode can be configured through EditorConfig files or MSBuild properties to control source generation behavior and optimize for your specific scenarios.

## EditorConfig Configuration

Add TUnit configuration to your `.editorconfig` file for fine-grained control:

```ini
# TUnit Configuration
# For more information about TUnit configuration options, see:
# https://github.com/thomhurst/TUnit/docs/configuration

# Generic Type Resolution
# Controls the maximum depth for generic type resolution in AOT scenarios
# Default: 5
# Range: 1-20
tunit.generic_depth_limit = 5

# AOT-Only Mode
# Enforces AOT-only mode, disabling all reflection fallbacks
# Default: true
tunit.aot_only_mode = true

# Property Injection
# Enables dependency injection via property setters in test classes
# Default: true
tunit.enable_property_injection = true

# ValueTask Hook Support
# Enables ValueTask return types in hook methods for better performance
# Default: true
tunit.enable_valuetask_hooks = true

# Verbose Diagnostics
# Enables verbose diagnostic messages from the source generator
# Default: false
tunit.enable_verbose_diagnostics = false

# Maximum Generic Instantiations
# Controls the maximum number of generic instantiations per type
# Default: 10
# Range: 1-100
tunit.max_generic_instantiations = 10

# Automatic Generic Discovery
# Enables automatic discovery of generic test instantiations
# Default: true
tunit.enable_auto_generic_discovery = true
```

## MSBuild Configuration

Configure TUnit through MSBuild properties in your `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <!-- TUnit AOT Configuration -->
    <TUnitAotOnlyMode>true</TUnitAotOnlyMode>
    <TUnitGenericDepthLimit>8</TUnitGenericDepthLimit>
    <TUnitEnablePropertyInjection>true</TUnitEnablePropertyInjection>
    <TUnitEnableValueTaskHooks>true</TUnitEnableValueTaskHooks>
    <TUnitEnableVerboseDiagnostics>false</TUnitEnableVerboseDiagnostics>
    <TUnitMaxGenericInstantiations>15</TUnitMaxGenericInstantiations>
    <TUnitEnableAutoGenericDiscovery>true</TUnitEnableAutoGenericDiscovery>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="TUnit" Version="*" />
  </ItemGroup>

</Project>
```

## Configuration Options Reference

### Generic Type Resolution

#### `generic_depth_limit` / `TUnitGenericDepthLimit`
- **Type**: Integer
- **Default**: 5
- **Range**: 1-20
- **Description**: Controls how deeply nested generic types can be resolved at compile time
- **Performance Impact**: Higher values increase compilation time but support more complex generic scenarios

**Example scenarios by depth:**
- **Depth 1**: `List<T>`, `Dictionary<K,V>`
- **Depth 3**: `Dictionary<string, List<MyClass<T>>>`
- **Depth 5**: Complex nested generic collections with multiple type parameters

#### `max_generic_instantiations` / `TUnitMaxGenericInstantiations`
- **Type**: Integer  
- **Default**: 10
- **Range**: 1-100
- **Description**: Maximum number of generic instantiations per generic type
- **Performance Impact**: Higher values support more type combinations but increase binary size

#### `enable_auto_generic_discovery` / `TUnitEnableAutoGenericDiscovery`
- **Type**: Boolean
- **Default**: true
- **Description**: Automatically discover generic type usage and generate appropriate instantiations
- **When to disable**: For explicit control over which generic types are generated

### Performance and Features

#### `aot_only_mode` / `TUnitAotOnlyMode`
- **Type**: Boolean
- **Default**: true
- **Description**: Enforces AOT-only execution, completely removing reflection fallbacks
- **Benefits**: Better performance, smaller binary size, Native AOT compatibility
- **Note**: Cannot be disabled in current version

#### `enable_property_injection` / `TUnitEnablePropertyInjection`
- **Type**: Boolean
- **Default**: true
- **Description**: Enables compile-time property injection with dependency resolution
- **When to disable**: If not using property injection, can reduce generated code size

#### `enable_valuetask_hooks` / `TUnitEnableValueTaskHooks`
- **Type**: Boolean
- **Default**: true
- **Description**: Supports ValueTask return types in hook methods for reduced allocations
- **Performance Impact**: ValueTask provides better performance for frequently-executed hooks

### Diagnostics and Debugging

#### `enable_verbose_diagnostics` / `TUnitEnableVerboseDiagnostics`
- **Type**: Boolean
- **Default**: false
- **Description**: Enables detailed diagnostic messages during source generation
- **Use cases**: Debugging source generation issues, understanding compilation behavior
- **Performance Impact**: Increases compilation time and log verbosity

## Environment-Specific Configuration

### Development Environment
Optimize for faster builds and better debugging:

```ini
# Development settings - faster builds
tunit.generic_depth_limit = 3
tunit.max_generic_instantiations = 5
tunit.enable_verbose_diagnostics = true
```

### Production/CI Environment
Optimize for performance and completeness:

```ini
# Production settings - full optimization
tunit.generic_depth_limit = 10
tunit.max_generic_instantiations = 20
tunit.enable_verbose_diagnostics = false
```

### Native AOT Publishing
For Native AOT scenarios, use conservative settings:

```ini
# Native AOT settings - maximum compatibility
tunit.generic_depth_limit = 5
tunit.max_generic_instantiations = 10
tunit.enable_auto_generic_discovery = false  # Use explicit [GenerateGenericTest]
```

## Common Configuration Patterns

### High-Performance Testing
```xml
<PropertyGroup>
  <!-- Maximize runtime performance -->
  <TUnitAotOnlyMode>true</TUnitAotOnlyMode>
  <TUnitEnableValueTaskHooks>true</TUnitEnableValueTaskHooks>
  <TUnitGenericDepthLimit>3</TUnitGenericDepthLimit>
  <TUnitMaxGenericInstantiations>5</TUnitMaxGenericInstantiations>
</PropertyGroup>
```

### Complex Generic Testing
```xml
<PropertyGroup>
  <!-- Support complex generic scenarios -->
  <TUnitGenericDepthLimit>10</TUnitGenericDepthLimit>
  <TUnitMaxGenericInstantiations>25</TUnitMaxGenericInstantiations>
  <TUnitEnableAutoGenericDiscovery>true</TUnitEnableAutoGenericDiscovery>
</PropertyGroup>
```

### Minimal Code Generation
```xml
<PropertyGroup>
  <!-- Generate minimal code for smaller binaries -->
  <TUnitEnablePropertyInjection>false</TUnitEnablePropertyInjection>
  <TUnitEnableValueTaskHooks>false</TUnitEnableValueTaskHooks>
  <TUnitGenericDepthLimit>1</TUnitGenericDepthLimit>
  <TUnitMaxGenericInstantiations>3</TUnitMaxGenericInstantiations>
</PropertyGroup>
```

## Troubleshooting Configuration

### Compilation Time Issues
If builds are too slow:
1. Reduce `generic_depth_limit` to 3 or lower
2. Reduce `max_generic_instantiations` to 5 or lower  
3. Set `enable_auto_generic_discovery = false` and use explicit `[GenerateGenericTest]`
4. Disable `enable_verbose_diagnostics`

### Binary Size Issues
If published applications are too large:
1. Use minimal configuration pattern above
2. Review and remove unused generic instantiations
3. Consider disabling property injection if not needed

### Generic Type Support Issues
If generic tests aren't working:
1. Increase `generic_depth_limit` to 8-10
2. Increase `max_generic_instantiations` to 15-25
3. Enable `enable_verbose_diagnostics` to see generation details
4. Add explicit `[GenerateGenericTest]` attributes for complex scenarios

### AOT Compatibility Issues
For Native AOT publishing problems:
1. Enable `enable_verbose_diagnostics` to identify AOT warnings
2. Use explicit generic instantiation instead of auto-discovery
3. Ensure all data sources use static methods
4. Avoid reflection APIs in test code (use diagnostics to find issues)