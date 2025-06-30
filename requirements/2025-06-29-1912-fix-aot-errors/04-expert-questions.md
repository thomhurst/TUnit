# Expert Requirements Questions

Based on my analysis of the TUnit codebase and the AOT compilation errors, I have these technical questions to clarify the exact implementation approach:

## 1. Compile-Time Method Resolution Strategy

**Question**: For `MethodDataSourceAttribute`, when should we attempt compile-time resolution vs. fall back to dynamic resolution?

**Context**: I can see that methods could be:
- Static methods with no parameters returning `IEnumerable<object[]>`
- Instance methods requiring object instantiation
- Methods with complex return types requiring conversion
- Generic methods requiring type resolution

**Specific sub-questions**:
- Should we only resolve static, parameterless methods at compile time?
- How should we handle methods that return `IEnumerable<T>` vs `IEnumerable<object[]>`?
- Should we attempt to instantiate classes with parameterless constructors for instance methods?

## 2. Error Handling Strategy

**Question**: How should we handle compile-time evaluation failures?

**Context**: When attempting to resolve a method at compile time, various errors could occur:
- Method not found
- Method throws exception during execution
- Return type incompatible with test parameters
- Circular dependencies

**Options**:
A) Generate compilation error (breaking change)
B) Fall back to `DynamicTestDataSource` silently
C) Generate warning and fall back to `DynamicTestDataSource`
D) Generate `StaticTestDataSource` with error placeholder

## 3. AsyncDataSourceGeneratorAttribute Warning Strategy

**Question**: What specific warning attributes should be added to `AsyncDataSourceGeneratorAttribute`?

**Context**: This attribute is inherently dynamic and cannot be made AOT-compatible.

**Options**:
A) Just `[RequiresDynamicCode]`
B) Just `[RequiresUnreferencedCode]` 
C) Both attributes
D) Custom warning message with specific guidance

## 4. Backward Compatibility Testing

**Question**: Should we implement feature flags or gradual rollout?

**Context**: This change affects core test discovery and could have subtle behavioral differences.

**Options**:
A) Enable new behavior by default with opt-out
B) Enable via feature flag with opt-in
C) Automatic detection based on compilation mode (AOT vs reflection)
D) Always attempt static resolution first, fall back automatically

## 5. Performance Considerations

**Question**: Should compile-time method execution be cached or limited?

**Context**: Methods could be expensive to execute or have side effects.

**Considerations**:
- Methods might access file system, network, or other external resources
- Methods might be slow (database queries, complex calculations)
- Methods might have side effects (logging, state changes)
- How to detect and handle these scenarios?

## 6. Type Safety and Conversion

**Question**: How should we handle type conversions for compile-time resolved data?

**Context**: Static methods might return `IEnumerable<int>` but test method expects `object[]`.

**Specific cases**:
- Primitive type boxing
- Tuple unwrapping for multiple parameters
- Custom object serialization
- Null handling and nullable types

## 7. Integration with Existing Pipeline

**Question**: Should the changes be isolated to `CodeGenerationHelpers.cs` or require broader changes?

**Context**: The current architecture has separate concerns between source generation and runtime.

**Areas to consider**:
- Does `DataSourceExpander.cs` need modifications?
- Should we create new test data source types?
- How does this integrate with the unified test builder architecture?

Please provide your preferred approaches for these questions to ensure the implementation meets your expectations.