# ITestRegistry Interface Implementation Summary

## Changes Made

### 1. Created ITestRegistry Interface (TUnit.Core)
- Added `ITestRegistry.cs` in `TUnit.Core/Interfaces/`
- Defines the contract for dynamic test registration with a single method:
  ```csharp
  Task AddDynamicTest<T>(TestContext context, DynamicTestInstance<T> dynamicTest) where T : class;
  ```
- Includes proper `DynamicallyAccessedMembers` attributes for AOT compatibility

### 2. Updated TestRegistry Implementation (TUnit.Engine)
- Modified `TestRegistry` class to implement `ITestRegistry`
- Changed from singleton pattern to constructor-based dependency injection
- Constructor now accepts all required dependencies:
  - `TestBuilderPipeline`
  - `IMessageBus`
  - `ISingleTestExecutor`
  - Session ID
  - `CancellationToken`

### 3. Updated TestContextExtensions (TUnit.Core)
- Simplified `AddDynamicTest` method to use dependency injection:
  ```csharp
  await context.GetService<ITestRegistry>()!.AddDynamicTest(context, dynamicTest);
  ```
- Removed complex reflection-based approach (kept as fallback for backward compatibility)
- Now uses the cleaner service provider pattern

### 4. Service Registration (TUnit.Engine)
- In `TUnitServiceProvider`, the `TestRegistry` is now properly registered as `ITestRegistry`:
  ```csharp
  Register<ITestRegistry>(new TestRegistry(TestBuilderPipeline, messageBus, singleTestExecutor, TestSessionId, CancellationToken.Token));
  ```

### 5. Removed Initialization Pattern
- Previously, `TestExecutor` had to initialize `TestRegistry.Instance`
- Now, `TestRegistry` is created with all dependencies during service provider construction
- This eliminates the temporal coupling and makes the code more testable

## Benefits

1. **Cleaner Architecture**: The interface provides a clear contract for dynamic test registration
2. **Better Testability**: Dependencies are injected rather than using singletons
3. **Type Safety**: No more reflection needed in the common path
4. **AOT Friendly**: Proper annotations ensure compatibility with trimming
5. **Simpler Code**: The extension method is now just one line in the happy path

## Usage

Dynamic tests can still be added the same way:
```csharp
await context.AddDynamicTest(new DynamicTestInstance<MyTestClass>
{
    TestMethod = @class => @class.SomeMethod(args),
    TestClassArguments = [...],
    TestMethodArguments = [...],
    Attributes = [new RepeatAttribute(5)]
});
```

But now the underlying implementation is cleaner and more maintainable.