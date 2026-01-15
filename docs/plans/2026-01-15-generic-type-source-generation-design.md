# Generic Type Source Generation Design

**Date:** 2026-01-15
**Status:** Proposed
**Issue:** #4431
**PR:** #4434

## Problem Statement

The `PropertyInjectionSourceGenerator` currently skips generic types entirely:

```csharp
// PropertyInjectionSourceGenerator.cs lines 103-105
if (containingType.IsUnboundGenericType || containingType.TypeParameters.Length > 0)
    return null;
```

This means generic types like `CustomWebApplicationFactory<TProgram>` never get source-generated metadata for:
- Properties with `IDataSourceAttribute` (e.g., `[ClassDataSource<T>]`)
- Nested `IAsyncInitializer` properties

**Impact:**
- In non-AOT scenarios, the reflection fallback works but is suboptimal
- In AOT scenarios, this is completely broken - no metadata means no initialization

## Solution Overview

Generate source metadata for **concrete instantiations** of generic types discovered at compile time, while keeping the reflection fallback for runtime-only types.

### Discovery Sources

1. **Inheritance chains** - `class MyTests : GenericBase<ConcreteType>`
2. **`IDataSourceAttribute` type arguments** - `[SomeDataSource<GenericType<Concrete>>]`
3. **Base type arguments** - Walking up inheritance hierarchies

### Key Principle

Once we discover a concrete type like `CustomWebApplicationFactory<Program>`, we treat it identically to a non-generic type for code generation.

## Architecture

### Current State

```
PropertyInjectionSourceGenerator
├── Pipeline 1: Property Data Sources (non-generic types only)
└── Pipeline 2: IAsyncInitializer Properties (non-generic types only)
```

### Proposed State

```
PropertyInjectionSourceGenerator
├── Pipeline 1: Property Data Sources (non-generic types)
├── Pipeline 2: IAsyncInitializer Properties (non-generic types)
├── Pipeline 3: Concrete Generic Type Discovery
│   └── Scans compilation for all concrete instantiations
├── Pipeline 4: Generic Property Data Sources
│   └── Generates PropertySource for concrete generic types
└── Pipeline 5: Generic IAsyncInitializer Properties
    └── Generates InitializerPropertyRegistry for concrete generic types
```

## Detailed Design

### Pipeline 3: Concrete Generic Type Discovery

**Model:**

```csharp
record ConcreteGenericTypeModel
{
    INamedTypeSymbol ConcreteType { get; }      // e.g., CustomWebApplicationFactory<Program>
    INamedTypeSymbol GenericDefinition { get; } // e.g., CustomWebApplicationFactory<>
    string SafeTypeName { get; }                // For file naming
}
```

**Discovery Implementation:**

```csharp
var concreteGenericTypes = context.SyntaxProvider
    .CreateSyntaxProvider(
        predicate: static (node, _) => node is TypeDeclarationSyntax or PropertyDeclarationSyntax,
        transform: static (ctx, _) => ExtractConcreteGenericTypes(ctx))
    .Where(static x => x.Length > 0)
    .SelectMany(static (x, _) => x)
    .Collect()
    .Select(static (types, _) => types.Distinct(SymbolEqualityComparer.Default));
```

**Discovery from Inheritance:**

```csharp
private static IEnumerable<INamedTypeSymbol> DiscoverFromInheritance(INamedTypeSymbol type)
{
    var baseType = type.BaseType;
    while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
    {
        if (baseType.IsGenericType && !baseType.IsUnboundGenericType)
        {
            yield return baseType; // Concrete generic like GenericBase<string>
        }
        baseType = baseType.BaseType;
    }
}
```

**Discovery from IDataSourceAttribute:**

```csharp
private static IEnumerable<INamedTypeSymbol> DiscoverFromAttributes(
    IPropertySymbol property,
    INamedTypeSymbol dataSourceInterface)
{
    foreach (var attr in property.GetAttributes())
    {
        if (attr.AttributeClass?.AllInterfaces.Contains(dataSourceInterface) != true)
            continue;

        // Check attribute type arguments
        if (attr.AttributeClass is { IsGenericType: true, TypeArguments.Length: > 0 })
        {
            foreach (var typeArg in attr.AttributeClass.TypeArguments)
            {
                if (typeArg is INamedTypeSymbol { IsGenericType: true, IsUnboundGenericType: false } concreteGeneric)
                {
                    yield return concreteGeneric;
                }
            }
        }
    }
}
```

### Pipeline 4 & 5: Generation for Concrete Generic Types

Reuses the same generation logic as Pipelines 1 & 2, just with concrete generic types.

**Example Generated Output:**

```csharp
// For CustomWebApplicationFactory<Program>
internal static class CustomWebApplicationFactory_Program_PropertyInjectionInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        PropertySourceRegistry.Register(
            typeof(CustomWebApplicationFactory<Program>),
            new CustomWebApplicationFactory_Program_PropertySource());
    }
}

internal sealed class CustomWebApplicationFactory_Program_PropertySource : IPropertySource
{
    public Type Type => typeof(CustomWebApplicationFactory<Program>);
    public bool ShouldInitialize => true;

    public IEnumerable<PropertyInjectionMetadata> GetPropertyMetadata()
    {
        yield return new PropertyInjectionMetadata
        {
            PropertyName = "Postgres",
            PropertyType = typeof(InMemoryPostgres),
            ContainingType = typeof(CustomWebApplicationFactory<Program>),
            CreateDataSource = () => new ClassDataSourceAttribute<InMemoryPostgres>
            {
                Shared = SharedType.PerTestSession
            },
            SetProperty = (instance, value) =>
                ((CustomWebApplicationFactory<Program>)instance).Postgres = (InMemoryPostgres)value
        };
    }
}
```

### Deduplication

The same concrete type might be discovered from multiple sources. Deduplication uses `SymbolEqualityComparer.Default` on the collected types before generation.

**Safe File Naming:**

```csharp
private static string GetSafeTypeName(INamedTypeSymbol concreteType)
{
    var fullName = concreteType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    return fullName
        .Replace("global::", "")
        .Replace("<", "_")
        .Replace(">", "")
        .Replace(",", "_")
        .Replace(".", "_")
        .Replace(" ", "");
}
```

### Inheritance Chain Handling

When discovering `CustomWebApplicationFactory<Program>`, also generate for generic base types:

```
CustomWebApplicationFactory<Program>
    └── TestWebApplicationFactory<Program>  (generate metadata)
        └── WebApplicationFactory<Program>  (generate metadata if relevant)
```

## Implementation Plan

### Phase 1: Core Discovery Infrastructure
1. Create `ConcreteGenericTypeDiscoverer` helper class
2. Implement discovery from inheritance chains
3. Implement discovery from `IDataSourceAttribute` type arguments
4. Add deduplication logic

### Phase 2: Extend PropertyInjectionSourceGenerator
1. Add Pipeline 3: Concrete generic type collection
2. Add Pipeline 4: Generic property data source generation
3. Add Pipeline 5: Generic IAsyncInitializer property generation
4. Update safe file naming for generic type arguments

### Phase 3: Handle Inheritance Chains
1. Walk up base types when discovering concrete generic type
2. Construct concrete version of each generic base type
3. Generate metadata for each hierarchy level

### Phase 4: Testing
1. Source generator unit tests for generic type scenarios
2. Integration tests for end-to-end behavior
3. Specific test for issue #4431 scenario
4. AOT compatibility verification

### Phase 5: Cleanup
1. Update PR #4434 with complete fix
2. Update documentation if needed

## Testing Strategy

### Unit Tests (Source Generator)

```csharp
// Generic class with IDataSourceAttribute property
[Fact]
public async Task GenericClass_WithDataSourceProperty_GeneratesMetadata();

// Generic class implementing IAsyncInitializer
[Fact]
public async Task GenericClass_ImplementingIAsyncInitializer_GeneratesMetadata();

// Discovery via inheritance
[Fact]
public async Task Discovery_ViaInheritance_FindsConcreteType();

// Discovery via IDataSourceAttribute type argument
[Fact]
public async Task Discovery_ViaDataSourceAttribute_FindsConcreteType();

// Nested generics
[Fact]
public async Task Discovery_NestedGenerics_FindsAllConcreteTypes();

// Inheritance chain walking
[Fact]
public async Task Discovery_WalksInheritanceChain_FindsBaseTypes();

// Deduplication
[Fact]
public async Task Discovery_DuplicateUsages_GeneratesOnce();
```

### Integration Tests (Engine)

```csharp
// Issue #4431 scenario
[Fact]
public async Task GenericWebApplicationFactory_InitializesNestedInitializers();

// Shared data source with generic fixture
[Fact]
public async Task GenericFixture_SharedDataSource_InitializedBeforeTest();

// Multiple instantiations
[Fact]
public async Task SameGeneric_DifferentTypeArgs_BothWork();
```

### AOT Verification

```csharp
// Verify source-gen metadata exists
[Fact]
public async Task GenericTypes_HaveSourceGenMetadata_NoReflectionFallback();
```

## File Changes

- `PropertyInjectionSourceGenerator.cs` - Major changes (new pipelines)
- New: `ConcreteGenericTypeDiscoverer.cs` - Discovery helper
- New: `ConcreteGenericTypeModel.cs` - Model for discovered types
- New tests in `TUnit.Core.SourceGenerator.Tests`
- New tests in `TUnit.Engine.Tests`

## Backward Compatibility

- Fully backward compatible
- Non-generic types continue to work unchanged
- Generic types that previously fell back to reflection now get source-gen metadata
- Reflection fallback remains for runtime-only types (non-AOT scenarios)

## Open Questions

None - design is complete and ready for implementation.
