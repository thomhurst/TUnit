# Source Generator Overhaul Design

**Date:** 2026-01-14
**Status:** Approved
**Problem:** Build times increased from ~2s to ~20s (11x regression)

## Root Cause Analysis

### Primary Issue: Storing Roslyn Symbols in Models

Multiple generators store `ISymbol`, `SyntaxNode`, `SemanticModel`, and `GeneratorAttributeSyntaxContext` in models that flow through the incremental pipeline. These types cannot be properly cached by Roslyn because they reference the `Compilation` object which changes on every keystroke.

**Affected Models:**

| Model | Problematic Fields |
|-------|-------------------|
| `TestMethodMetadata` | `IMethodSymbol`, `INamedTypeSymbol`, `MethodDeclarationSyntax`, `GeneratorAttributeSyntaxContext`, `AttributeData[]` |
| `HooksDataModel` | `GeneratorAttributeSyntaxContext`, `IMethodSymbol`, `INamedTypeSymbol` |
| `InheritsTestsClassMetadata` | `INamedTypeSymbol`, `ClassDeclarationSyntax`, `GeneratorAttributeSyntaxContext` |
| `PropertyInjectionContext` | `INamedTypeSymbol` |
| `PropertyWithDataSource` | `IPropertySymbol`, `AttributeData` |

### Secondary Issues

1. **Broad Syntax Predicates:** `PropertyInjectionSourceGenerator` and `StaticPropertyInitializationGenerator` use `CreateSyntaxProvider` matching ALL classes instead of targeting specific attributes.

2. **Full Compilation Scanning:** `AotConverterGenerator` iterates through ALL syntax trees on every compilation change.

3. **Non-Deterministic Output:** Three generators use `Guid.NewGuid()` in filenames or class names, preventing caching.

## Solution: The "Extracted Data" Pattern

### Core Principle

All symbol analysis happens in the `transform` function. Models contain ONLY:
- Primitive types (`string`, `int`, `bool`, `enum`)
- Arrays/collections of primitives
- Other "extracted data" models
- **NEVER** `ISymbol`, `SyntaxNode`, `SemanticModel`, `Compilation`, or `GeneratorAttributeSyntaxContext`

### Pipeline Pattern

```
ForAttributeWithMetadataName("Attribute.Name")
    ↓
Transform: Extract ALL data as primitives
    ↓
Combine with enabledProvider
    ↓
RegisterSourceOutput: Generate code using only primitives
```

## Proposed Architecture

### Generator Count: 9 → 5

| Generator | Responsibility |
|-----------|---------------|
| `TestMetadataGenerator` | Test discovery, registration, AND AOT converters |
| `HookMetadataGenerator` | All hook types (Before/After × Each/Every × Assembly/Class/Test) |
| `PropertyDataSourceGenerator` | Instance + static property injection (unified) |
| `DynamicTestsGenerator` | Runtime-generated tests via [DynamicTestSource] |
| `InfrastructureGenerator` | Module initializer setup + assembly loading |

### Moved to TUnit.Analyzers

| Analyzer | Responsibility |
|----------|---------------|
| `LanguageVersionAnalyzer` | Reports error if C# < 12 |

## New Model Definitions

### TestMethodModel

```csharp
public sealed class TestMethodModel : IEquatable<TestMethodModel>
{
    // Type identity
    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string Namespace { get; init; }
    public required string AssemblyName { get; init; }

    // Method identity
    public required string MethodName { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }

    // Generics (extracted as strings)
    public required bool IsGenericType { get; init; }
    public required bool IsGenericMethod { get; init; }
    public required EquatableArray<string> TypeParameters { get; init; }
    public required EquatableArray<string> MethodTypeParameters { get; init; }
    public required EquatableArray<string> TypeConstraints { get; init; }

    // Method signature
    public required string ReturnType { get; init; }
    public required EquatableArray<ParameterModel> Parameters { get; init; }

    // Attributes (fully extracted)
    public required EquatableArray<ExtractedAttribute> Attributes { get; init; }

    // Data sources
    public required EquatableArray<DataSourceModel> DataSources { get; init; }

    // AOT converters (integrated)
    public required EquatableArray<string> TypesNeedingConverters { get; init; }

    // Inheritance
    public required int InheritanceDepth { get; init; }
}
```

### HookModel

```csharp
public sealed class HookModel : IEquatable<HookModel>
{
    // Identity
    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string MethodName { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }

    // Hook configuration
    public required HookLevel Level { get; init; }
    public required HookType Type { get; init; }
    public required HookTiming Timing { get; init; }
    public required int Order { get; init; }
    public required string? HookExecutorTypeName { get; init; }

    // Method info
    public required bool IsStatic { get; init; }
    public required bool IsAsync { get; init; }
    public required bool ReturnsVoid { get; init; }
    public required EquatableArray<string> ParameterTypes { get; init; }

    // Class info
    public required bool ClassIsStatic { get; init; }
    public required EquatableArray<string> ClassTypeParameters { get; init; }
}

public enum HookLevel { Assembly, Class, Test }
public enum HookType { Before, After }
public enum HookTiming { Each, Every }
```

### PropertyDataModel

```csharp
public sealed class PropertyDataModel : IEquatable<PropertyDataModel>
{
    // Property identity
    public required string PropertyName { get; init; }
    public required string PropertyTypeName { get; init; }
    public required string ContainingTypeName { get; init; }
    public required string MinimalContainingTypeName { get; init; }
    public required string Namespace { get; init; }

    // Property characteristics
    public required bool IsStatic { get; init; }
    public required bool HasPublicSetter { get; init; }

    // Data source (extracted)
    public required DataSourceModel DataSource { get; init; }
}
```

### Supporting Models

```csharp
public sealed class ExtractedAttribute : IEquatable<ExtractedAttribute>
{
    public required string FullyQualifiedName { get; init; }
    public required EquatableArray<TypedConstantModel> ConstructorArguments { get; init; }
    public required EquatableArray<NamedArgumentModel> NamedArguments { get; init; }
}

public sealed class TypedConstantModel : IEquatable<TypedConstantModel>
{
    public required string TypeName { get; init; }
    public required string? Value { get; init; }
    public required TypedConstantKind Kind { get; init; }
    public required EquatableArray<TypedConstantModel>? ArrayValues { get; init; }
}

public sealed class NamedArgumentModel : IEquatable<NamedArgumentModel>
{
    public required string Name { get; init; }
    public required TypedConstantModel Value { get; init; }
}

public sealed class ParameterModel : IEquatable<ParameterModel>
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public required bool HasDefaultValue { get; init; }
    public required string? DefaultValue { get; init; }
}

public sealed class DataSourceModel : IEquatable<DataSourceModel>
{
    public required DataSourceKind Kind { get; init; }
    public required string? MethodName { get; init; }
    public required string? ContainingTypeName { get; init; }
    public required EquatableArray<TypedConstantModel> Arguments { get; init; }
    public required ExtractedAttribute SourceAttribute { get; init; }
}
```

### EquatableArray Utility

```csharp
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[] _array;

    public EquatableArray(T[] array) => _array = array ?? Array.Empty<T>();

    public bool Equals(EquatableArray<T> other)
    {
        if (_array.Length != other._array.Length)
            return false;

        for (int i = 0; i < _array.Length; i++)
        {
            if (!_array[i].Equals(other._array[i]))
                return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (var item in _array)
                hash = hash * 31 + item.GetHashCode();
            return hash;
        }
    }

    // IEnumerable implementation...
}
```

## Property Data Source Handling

Since users can create custom data source attributes, `ForAttributeWithMetadataName` cannot be used. Instead:

```csharp
// Target properties with any attribute
predicate: static (s, _) => s is PropertyDeclarationSyntax { AttributeLists.Count: > 0 }

// Then check inheritance in transform
static bool IsDataSourceAttribute(INamedTypeSymbol? attrType)
{
    while (attrType != null)
    {
        var name = attrType.ToDisplayString();
        if (name is "TUnit.Core.DataSourceGeneratorAttribute"
                 or "TUnit.Core.ArgumentsAttribute"
                 or "TUnit.Core.MethodDataSourceAttribute"
                 /* etc */)
            return true;
        attrType = attrType.BaseType;
    }
    return false;
}
```

## InfrastructureGenerator (Consolidated)

Combines `DisableReflectionScannerGenerator` and `AssemblyLoaderGenerator`:

```csharp
file static class TUnitInfrastructure
{
    [ModuleInitializer]
    public static void Initialize()
    {
        global::TUnit.Core.SourceRegistrar.IsEnabled = true;

        // Assembly loading
        global::TUnit.Core.SourceRegistrar.RegisterAssembly(() =>
            global::System.Reflection.Assembly.Load("AssemblyName, Version=..."));
        // ...
    }
}
```

Key improvements:
- Single deterministic output file
- No GUIDs (uses `file` keyword for collision avoidance)

## Implementation Plan

| Phase | Task | Risk | Impact |
|-------|------|------|--------|
| 1 | Create `EquatableArray<T>` and primitive model infrastructure | Low | Foundation |
| 2 | Fix `TestMetadataGenerator` - largest impact on build times | Medium | High |
| 3 | Fix `HookMetadataGenerator` | Medium | Medium |
| 4 | Unify and fix `PropertyDataSourceGenerator` | Medium | Medium |
| 5 | Fix `DynamicTestsGenerator` (remove GUID) | Low | Low |
| 6 | Create `InfrastructureGenerator` (consolidate utilities) | Low | Low |
| 7 | Move `LanguageVersionCheck` to Analyzers | Low | Low |
| 8 | Delete old generators and models | Low | Cleanup |

## Expected Outcomes

- Build times return to ~2-3 seconds range
- Incremental compilation works correctly (typing doesn't trigger full regeneration)
- Cleaner, more maintainable generator codebase
- Reduced generator count (9 → 5)

## Testing Strategy

1. Run existing snapshot tests after each phase
2. Benchmark build times after Phase 2 (TestMetadataGenerator)
3. Verify incremental compilation with IDE typing tests
4. Full test suite must pass before merging
