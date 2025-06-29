# Expert Requirements Answers

## Q1: Compile-Time Method Resolution Strategy
**Answer**: Resolve everything possible at compile time
- Attempt compile-time resolution for all method types (static, instance, generic, etc.)
- Handle methods returning various types (`IEnumerable<T>`, `IEnumerable<object[]>`, etc.)
- Instantiate classes with parameterless constructors for instance methods
- Be aggressive about compile-time resolution to maximize AOT compatibility

## Q2: Error Handling Strategy  
**Answer**: Option C - Generate warning and fall back to `DynamicTestDataSource`
- When compile-time evaluation fails, emit a compiler warning
- Fall back to `DynamicTestDataSource` for runtime resolution
- Maintains backward compatibility while providing feedback to developers
- Allows gradual migration without breaking existing tests

## Q3: AsyncDataSourceGeneratorAttribute Warning Strategy
**Answer**: Both `[RequiresDynamicCode]` and `[RequiresUnreferencedCode]` attributes + make generic overloads AOT-safe
- Apply both warning attributes to the base `AsyncDataSourceGeneratorAttribute`
- However, the strongly-typed generic overloads can be made AOT-compatible
- Focus on making the generic versions work with compile-time resolution
- Only the truly dynamic base scenarios need the warning attributes

## Q4: Backward Compatibility Strategy
**Answer**: Option D - Always attempt static resolution first, fall back automatically
- No feature flags or configuration needed
- Always try compile-time resolution first
- Automatically fall back to dynamic resolution if needed
- Provides benefits without requiring user configuration
- Transparent improvement to existing code

## Q5: Performance Considerations
**Answer**: Implement timeouts but no caching for test isolation
- Add timeouts to prevent slow methods from blocking builds
- Do NOT implement caching to maintain test isolation
- Each test should get unique instances of data where possible
- Prevents side effects between tests while protecting build performance

## Q6: Type Safety and Conversion
**Answer**: Handle conversions automatically using CastHelper class
- Automatically handle primitive type boxing
- Automatically handle tuple unwrapping for multiple parameters
- Leverage existing `CastHelper` class for type conversions
- Handle null values and nullable types automatically
- Make type conversion seamless for developers

## Q7: Integration Architecture
**Answer**: Choose cleanest architecture following DRY, SOLID, SRP, KISS principles
- Prioritize clean, maintainable, understandable code
- Follow DRY (Don't Repeat Yourself)
- Follow SOLID principles
- Follow Single Responsibility Principle (SRP)
- Follow KISS (Keep It Simple, Stupid)
- Make architectural decisions based on these principles rather than specific constraints