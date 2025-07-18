# Generic Test Issue - Root Cause and Solution

## Root Cause Analysis

After thorough investigation, the issue appears to be that for generic test classes like `SimpleGenericClassTests<T>`, the source generator is generating test metadata, but the `GetDataSourceAttributes` method is not finding the `ArgumentsAttribute` on the method symbol during source generation.

### Evidence:
1. **TestMetadataGenerator DOES process generic types** - confirmed by checking the code
2. **Test metadata IS generated** - the generator creates metadata with `IsGenericType = true`
3. **DataCombinationGenerator is emitted** - but `methodDataSources` is empty
4. **The debug output shows**: "Method data sources count: 0" and "Arguments attributes found: 0"

## The Problem

In `DataCombinationGeneratorEmitter.EmitDataCombinationGenerator`:
```csharp
var methodDataSources = GetDataSourceAttributes(methodSymbol);
```

For generic types, the `methodSymbol` seems to not have the attributes properly associated when retrieved during source generation. This is likely because:

1. The method symbol represents a generic method definition (`TestWithValue<T>`)
2. The attributes might be on a different representation of the symbol
3. There could be a timing issue in the Roslyn compilation model

## Proposed Solution

The issue might be that we need to look at the original method declaration to get the attributes, not the symbol that's been processed through the generic type resolution.

### Option 1: Check the syntax node for attributes
Instead of relying solely on `methodSymbol.GetAttributes()`, we could also check the syntax node:

```csharp
private static ImmutableArray<AttributeData> GetDataSourceAttributes(ISymbol symbol, SyntaxNode? syntaxNode = null)
{
    var attributes = symbol.GetAttributes()
        .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
        .ToList();
    
    // For generic types, also check syntax if available
    if (syntaxNode is MethodDeclarationSyntax methodSyntax && attributes.Count == 0)
    {
        // Parse attributes from syntax
        // This would require passing the syntax node through the chain
    }
    
    return attributes.ToImmutableArray();
}
```

### Option 2: Store attributes in TestMethodMetadata
Since `TestMethodMetadata` already has access to the attributes during `GetTestMethodMetadata`, we could store them:

```csharp
public class TestMethodMetadata
{
    // ... existing properties ...
    public ImmutableArray<AttributeData> MethodAttributes { get; set; }
}
```

Then in `GetTestMethodMetadata`:
```csharp
return new TestMethodMetadata
{
    // ... existing assignments ...
    MethodAttributes = methodSymbol.GetAttributes().ToImmutableArray()
};
```

And use these stored attributes in the DataCombinationGeneratorEmitter.

### Option 3: Debug and fix the actual issue
The debug logging I added should reveal what's happening. We need to see:
1. What attributes are on the method symbol
2. Why ArgumentsAttribute isn't being detected
3. If this is specific to generic types or a broader issue

## Next Steps

1. Run the test with the debug logging to see what attributes are found
2. Implement Option 2 (storing attributes in metadata) as it's the cleanest solution
3. Test thoroughly with both generic and non-generic types