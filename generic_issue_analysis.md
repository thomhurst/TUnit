# Generic Test Issue Analysis

## Problem Statement
When running `SimpleGenericClassTests<T>.TestWithValue(T value)` with `[Arguments(42)]`, the console output shows:
- "Method data sources count: 0"
- "Arguments attributes found: 0"

This happens on the first invocation but not on subsequent ones.

## Investigation Findings

### 1. Source Generation Flow
The TestMetadataGenerator does process generic types:
- `GetTestMethodMetadata` does NOT skip generic types
- It sets `IsGenericType = true` and continues processing
- `GenerateTestMethodSource` is called for generic test methods
- Test metadata IS generated for `SimpleGenericClassTests<T>`

### 2. Key Issue Identified
When generating test metadata for generic types:
- `TestClassType` is set to `typeof(object)` instead of the actual generic type definition
- This is done in `GenerateTypeReference` method:
  ```csharp
  if (isGeneric)
  {
      // For generic types, use typeof(object) as placeholder
      return "typeof(object)";
  }
  ```

### 3. DataCombinationGenerator
The DataCombinationGeneratorEmitter is properly emitted and includes:
- Debug logging for method data sources count
- Detection of ArgumentsAttribute for generic type resolution
- The issue is that `methodDataSources` is empty when retrieved

### 4. Root Cause Hypothesis
The problem appears to be that when `GetDataSourceAttributes` is called on the method symbol, it's not finding the `ArgumentsAttribute`. This could be because:

1. The method symbol context is different for generic types
2. The attribute might not be properly associated with the generic method symbol
3. There might be a timing issue where attributes aren't available on first access

### 5. Why It Works on Subsequent Runs
This suggests there might be:
- A caching mechanism that populates on first run
- Lazy initialization that completes after the first attempt
- State that persists between test runs

## Next Steps
1. Add more detailed logging in GetDataSourceAttributes
2. Check if the method symbol has the correct attributes attached
3. Investigate if there's a difference in how attributes are retrieved for generic vs non-generic types
4. Look at the actual generated code to see what's being produced