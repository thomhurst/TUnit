# Proposal: Generic Test Support in Source Generation Mode

## Overview

Currently, TUnit's source generation mode cannot handle generic test classes and methods, resulting in ~1000 fewer tests being discovered compared to reflection mode. This document outlines what would be required to achieve feature parity.

## Current Limitation

The source generator explicitly skips:
- Generic type definitions (e.g., `GenericTests<T>`)  
- Generic methods (e.g., `TestMethod<T>()`)
- Tests with async data source generators

This is because source generators run at compile time and cannot:
- Resolve generic type parameters without concrete type arguments
- Execute async data sources to determine type arguments
- Generate code that references open generic types directly

## Proposed Solution: Open Generic Metadata with Runtime Resolution

### Key Concept

Instead of skipping generic types, the source generator would:
1. Emit metadata for open generic types
2. Mark them as requiring runtime type resolution
3. Let the runtime resolver create concrete types during data expansion

### Implementation Steps

#### 1. Modify Source Generator to Emit Generic Metadata

```csharp
// Instead of skipping, emit special metadata
if (containingType.IsGenericType)
{
    return new TestMethodMetadata
    {
        IsGenericTypeDefinition = true,
        GenericParameterCount = containingType.TypeParameters.Length,
        RequiresRuntimeResolution = true,
        // ... other metadata
    };
}
```

#### 2. Generate Open Generic Type References

```csharp
// Instead of: TestClassType = typeof(GenericTests<int>)
// Generate: TestClassType = typeof(GenericTests<>).GetGenericTypeDefinition()
writer.AppendLine($"TestClassType = typeof({GetOpenGenericTypeName(className)}),");
writer.AppendLine("IsGenericTypeDefinition = true,");
writer.AppendLine($"GenericParameterCount = {typeParameters.Length},");
```

#### 3. Enhance TestMetadata to Support Generic Types

```csharp
public abstract class TestMetadata
{
    // New properties
    public bool IsGenericTypeDefinition { get; set; }
    public int GenericParameterCount { get; set; }
    public Type? GenericTypeDefinition { get; set; }
    
    // Modified to support creating concrete types
    public Func<Type[], TestMetadata> CreateConcreteMetadata { get; set; }
}
```

#### 4. Update Generic Type Resolver

The `SourceGeneratedGenericTypeResolver` would need to:

```csharp
public async Task<IEnumerable<TestMetadata>> ResolveGenericsAsync(IEnumerable<TestMetadata> metadata)
{
    var resolved = new List<TestMetadata>();
    
    foreach (var test in metadata)
    {
        if (!test.IsGenericTypeDefinition)
        {
            resolved.Add(test);
            continue;
        }
        
        // Extract class data sources that provide type arguments
        var typeArgSources = ExtractTypeArgumentDataSources(test);
        
        foreach (var typeArgs in typeArgSources)
        {
            // Create concrete metadata instance
            var concreteTest = test.CreateConcreteMetadata(typeArgs);
            resolved.Add(concreteTest);
        }
    }
    
    return resolved;
}
```

#### 5. Runtime Type Creation

During test execution, create concrete types:

```csharp
private Type CreateConcreteType(TestMetadata metadata, Type[] typeArguments)
{
    if (!metadata.IsGenericTypeDefinition)
        return metadata.TestClassType;
        
    return metadata.GenericTypeDefinition.MakeGenericType(typeArguments);
}
```

### Challenges

1. **Type Safety**: Source generators cannot validate that type arguments satisfy generic constraints at compile time
2. **Reflection Dependency**: Even in "source generation" mode, we'd need some reflection for `MakeGenericType`
3. **Complex Generic Scenarios**: Nested generics, method type parameters, and constraint validation
4. **Performance**: Runtime type creation adds overhead

### Alternative Approaches

#### 1. Partial Source Generation
- Generate metadata for non-generic parts
- Fall back to reflection for generic types only
- Maintains most AOT benefits while supporting generics

#### 2. Build-Time Type Resolution
- Require explicit type argument specifications via attributes
- Generate all concrete combinations at build time
- Example: `[TestTypeArguments(typeof(int), typeof(string))]`

#### 3. Hybrid Mode
- Use source generation for known types
- Use reflection for discovered generic instantiations
- Best of both worlds but more complex

## Recommendation

Given the complexity and the fact that generic type resolution fundamentally requires runtime capabilities, I recommend:

1. **Short term**: Document the limitation clearly and suggest workarounds (explicit concrete test classes)
2. **Medium term**: Implement Partial Source Generation (Alternative #1) to minimize reflection usage
3. **Long term**: Consider if full generic support aligns with AOT compilation goals

## Workaround for Users

Until generic support is added, users can:

```csharp
// Instead of:
public class GenericTests<T>
{
    [Test]
    public void TestMethod(T value) { }
}

// Create explicit concrete classes:
public class IntTests : GenericTests<int> { }
public class StringTests : GenericTests<string> { }
```

This maintains AOT compatibility while allowing generic test reuse through inheritance.