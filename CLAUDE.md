# Claude Instructions for TUnit

## IMPORTANT: Read This First

When assisting with TUnit development:
1. **ALWAYS** maintain behavioral parity between source-generated and reflection modes
2. **ALWAYS** run `dotnet test TUnit.PublicAPI` when modifying public APIs in TUnit.Core, TUnit.Engine, or TUnit.Playwright
3. **ALWAYS** run source generator tests when changing source generator output
4. **NEVER** use VSTest APIs - use Microsoft.Testing.Platform instead
5. **PREFER** performance over convenience - this framework may be used by millions
6. **FOLLOW** modern C# patterns and the coding standards outlined below

## Critical Testing Requirements

### Source Generator Changes
When modifying what the source generator emits:
1. **Run the source generator tests**:
   ```bash
   dotnet test TUnit.Core.SourceGenerator.Tests
   ```
2. **Accept any changed snapshots**:
   - Review the `.received.txt` files to ensure changes are intentional
   - Rename `.received.txt` files to `.verified.txt` to accept changes
   - Commit the updated snapshots with your changes

### Public API Changes
When modifying public APIs in TUnit.Core, TUnit.Engine, or TUnit.Playwright:
1. **Run the public API tests**:
   ```bash
   dotnet test TUnit.PublicAPI
   ```
2. **Accept new snapshots**:
   - Review the `.received.txt` files showing the current API surface
   - Ensure changes are intentional and necessary
   - Rename `.received.txt` files to `.verified.txt` to accept changes
   - Commit the updated snapshots with your changes

## Repository Overview

TUnit is a modern .NET testing framework designed as an alternative to xUnit, NUnit, and MSTest. It leverages the newer Microsoft.Testing.Platform instead of the legacy VSTest framework, providing improved performance and modern .NET capabilities.

## Key Architecture Concepts

### Dual Execution Modes
TUnit operates in two distinct execution modes that must maintain behavioral parity:

1. **Source Generated Mode**: Uses compile-time source generation for optimal performance
2. **Reflection Mode**: Uses runtime reflection for dynamic scenarios

**Critical Rule**: Both modes must produce identical end-user behavior. Any feature or bug fix must be implemented consistently across both execution paths.

### Core Components
- **TUnit.Core**: Core abstractions and interfaces
- **TUnit.Engine**: Test discovery and execution engine
- **TUnit.Core.SourceGenerator**: Compile-time test generation
- **TUnit.Assertions**: Fluent assertion library
- **TUnit.Analyzers**: Roslyn analyzers for compile-time validation

## Coding Standards and Best Practices

### .NET Modern Syntax
- Use collection initializers: `List<string> list = []` instead of `List<string> list = new()`
- Leverage pattern matching, records, and modern C# features
- Use file-scoped namespaces where appropriate
- Prefer `var` for local variables when type is obvious

### Code Formatting
- Always use braces for control structures, even single-line statements:
  ```csharp
  if (condition)
  {
      DoSomething();
  }
  ```
- Maintain consistent spacing between methods and logical code blocks
- Use expression-bodied members for simple properties and methods
- Follow standard .NET naming conventions (PascalCase for public members, _camelCase for private fields)
- Don't pollute code with unnecessary comments; use meaningful names instead

### Performance Considerations
- **Critical**: TUnit may be used by millions of developers - performance is paramount
- Avoid unnecessary allocations in hot paths
- Use `ValueTask` over `Task` for potentially synchronous operations
- Leverage object pooling for frequently created objects
- Consider memory usage patterns, especially in test discovery and execution

### Architecture Principles
- **Single Responsibility Principle**: Classes should have one reason to change
- **Avoid Over-engineering**: Prefer simple, maintainable solutions
- **Composition over Inheritance**: Use dependency injection and composition patterns
- **Immutability**: Prefer immutable data structures where possible

## Testing Framework Specifics

### Microsoft.Testing.Platform Integration
- Use Microsoft.Testing.Platform APIs instead of VSTest
- Test filtering syntax: `dotnet test -- --treenode-filter /Assembly/Namespace/ClassName/TestName`
- Understand the platform's execution model and lifecycle hooks

### Data Generation and Attributes
- Support multiple data source attributes: `[Arguments]`, `[MethodDataSource]`, `[ClassDataSource]`, etc.
- Reuse existing data generation logic instead of duplicating code
- Maintain consistency between reflection and source-generated approaches

### Test Discovery and Execution
- Test discovery should be fast and efficient
- Support dynamic test generation while maintaining type safety
- Handle edge cases gracefully (generic types, inheritance, etc.)

## Common Patterns and Conventions

### Error Handling
- Use specific exception types with meaningful messages
- Provide contextual information in error messages for debugging
- Handle reflection failures gracefully with fallback mechanisms

### Async/Await
- Use `ValueTask` for performance-critical async operations
- Properly handle `CancellationToken` throughout the pipeline
- Avoid async void except for event handlers

### Reflection Usage
- Use `[UnconditionalSuppressMessage]` attributes appropriately for AOT/trimming
- Cache reflection results where possible for performance
- Provide both reflection and source-generated code paths

## Code Review Guidelines

### When Adding New Features
1. Implement in both source-generated and reflection modes
2. Add corresponding analyzer rules if applicable
3. Include comprehensive tests covering edge cases
4. Verify performance impact with benchmarks if relevant
5. Update documentation and ensure API consistency

### When Fixing Bugs
1. Identify if the issue affects one or both execution modes
2. Write a failing test that reproduces the issue
3. Fix the bug in all affected code paths
4. Verify the fix doesn't introduce performance regressions

### Code Quality Checklist
- [ ] No unused using statements
- [ ] Proper null handling (nullable reference types)
- [ ] Appropriate access modifiers
- [ ] XML documentation for public APIs
- [ ] No magic strings or numbers (use constants)
- [ ] Proper disposal of resources
- [ ] Source generator tests run and snapshots accepted (if applicable)
- [ ] Public API tests run and snapshots accepted (if applicable)

## Testing and Validation

### Test Categories
- Unit tests for individual components
- Integration tests for cross-component functionality
- Performance benchmarks for critical paths
- Analyzer tests for compile-time validation
- Source generator snapshot tests
- Public API snapshot tests

### Compatibility Testing
- Test against multiple .NET versions (.NET 6, 8, 9+)
- Verify AOT and trimming compatibility
- Test source generation in various project configurations

## Common Gotchas and Pitfalls

1. **Execution Mode Inconsistency**: Always verify behavior matches between modes
2. **Performance Regressions**: Profile code changes in test discovery and execution
3. **AOT/Trimming Issues**: Be careful with reflection usage and dynamic code
4. **Thread Safety**: Ensure thread-safe patterns in concurrent test execution
5. **Memory Leaks**: Properly dispose of resources and avoid circular references
6. **Snapshot Test Failures**: Always review and accept snapshot changes when intentional

## Dependencies and Third-Party Libraries

- Minimize external dependencies for core functionality
- Use Microsoft.Extensions.* packages for common functionality
- Prefer .NET BCL types over third-party alternatives
- Keep analyzer dependencies minimal to avoid version conflicts

## Documentation Standards

- Use triple-slash comments for public APIs
- Include code examples in documentation
- Document performance characteristics for critical APIs
- Maintain README files for major components

## Questions to Ask When Making Changes

1. Does this change affect both execution modes?
2. Is this the most performant approach?
3. Does this follow established patterns in the codebase?
4. Are there any breaking changes or compatibility concerns?
5. How will this behave under high load or with many tests?
6. Does this require new analyzer rules or diagnostics?
7. Do source generator tests need updating?
8. Do public API tests need updating?

## IDE and Tooling

- Use EditorConfig settings for consistent formatting
- Leverage Roslyn analyzers for code quality
- Run performance benchmarks for critical changes
- Use the project's specific MSBuild properties and targets

## Critical Reminders for Every Change

### Before Submitting Any Code:
1. **Test Both Modes**: Verify your changes work identically in both source-generated and reflection modes
2. **Check Source Generator**: If changing source generator output, run tests and accept snapshots
3. **Check Public API**: If modifying TUnit.Core, TUnit.Engine, or TUnit.Playwright public APIs, run tests and accept snapshots
4. **Performance Impact**: Consider if your change affects test discovery or execution performance
5. **Breaking Changes**: Ensure no unintentional breaking changes to the public API
6. **Documentation**: Update relevant documentation for any public API changes

### Quick Commands Reference:
```bash
# Run all tests
dotnet test

# Run source generator tests
dotnet test TUnit.Core.SourceGenerator.Tests

# Run public API tests
dotnet test TUnit.PublicAPI

# Convert received to verified files (Windows)
for %f in (*.received.txt) do move /Y "%f" "%~nf.verified.txt"

# Convert received to verified files (Linux/macOS)
for file in *.received.txt; do mv "$file" "${file%.received.txt}.verified.txt"; done

# Run specific test
dotnet test -- --treenode-filter "/Assembly/Namespace/ClassName/TestName"
```

Remember: TUnit aims to be a fast, modern, and reliable testing framework. Every change should contribute to these goals while maintaining the simplicity and developer experience that makes testing enjoyable.