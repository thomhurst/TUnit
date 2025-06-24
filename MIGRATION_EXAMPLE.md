# Migration Example: Old vs New Architecture

## Test Discovery

### Old Architecture (Complex Pipeline)
```csharp
// Old: 4+ layers of delegation
TUnitTestDiscoverer
  → BaseTestsConstructor
    → TestsCollector
      → TestMetadataSource
        → TestVariationBuilder
          → Multiple builders (Static/Dynamic/Reflection)
```

### New Architecture (Direct)
```csharp
// New: Direct discovery
var discoveryService = new TestDiscoveryService(
    TestMetadataRegistry.GetSources(),
    new TestFactory(...),
    enableDynamicDiscovery: false);

var tests = await discoveryService.DiscoverTests();
```

## Test Creation

### Old Architecture (Multiple Builders)
```csharp
// Old: Different builders for different scenarios
if (canUseStatic)
{
    var builder = new StaticTestBuilder();
    var testDefinition = builder.Build(...);
}
else
{
    var builder = new DynamicTestBuilder();
    var testDefinition = builder.Build(...);
}

// Then convert to DiscoveredTest
var discoveredTest = TestVariationBuilder.CreateFrom(testDefinition);
```

### New Architecture (Single Factory)
```csharp
// New: One factory handles all scenarios
var testFactory = new TestFactory(testInvoker, hookInvoker, dataResolver);
var executableTests = await testFactory.CreateTests(metadata);
```

## Test Metadata

### Old Architecture (Multiple Types)
```csharp
// Old: Complex type hierarchy
TestDefinition baseDefinition;
TestDefinition<T> genericDefinition;
StaticTestDefinition staticDef;
DynamicTestMetadata dynamicMeta;

// Complex conversions between types
var converted = staticDef.ToDynamic();
```

### New Architecture (Unified)
```csharp
// New: Single metadata type
var metadata = new TestMetadata
{
    TestId = "Test.Method",
    TestName = "Method",
    TestClassType = typeof(TestClass),
    // AOT support built-in
    InstanceFactory = () => new TestClass(),
    TestInvoker = async (instance, args) => await InvokeTest(instance, args),
    // Reflection fallback
    MethodInfo = canUseAot ? null : methodInfo
};
```

## Test Execution

### Old Architecture (Complex Execution)
```csharp
// Old: Multiple executors and coordinators
var testsExecutor = new TestsExecutor(...);
var singleTestExecutor = new SingleTestExecutor(...);
var testInvoker = new TestInvoker(...);
var parallelCoordinator = new ParallelCoordinator(...);

await testsExecutor.ExecuteAsync(groupedTests, filter, token);
```

### New Architecture (Unified Executor)
```csharp
// New: Single executor with clear flow
var executor = new UnifiedTestExecutor(singleTestExecutor, options, logger);
await executor.ExecuteTests(tests, filter, messageBus, token);
```

## Source Generator

### Old Architecture
```csharp
// Old: Generates different types based on capabilities
if (context.CanUseStaticDefinition)
{
    // Generate StaticTestDefinition with complex initialization
}
else
{
    // Generate DynamicTestMetadata with different structure
}
```

### New Architecture
```csharp
// New: Always generates unified TestMetadata
_allTests.Add(new TestMetadata
{
    TestId = "TestClass.TestMethod",
    TestName = "TestMethod",
    TestClassType = typeof(TestClass),
    // AOT factories when possible
    InstanceFactory = canUseAot ? () => new TestClass() : null,
    TestInvoker = canUseAot ? TestClass_TestMethod_Invoker : null,
    // Reflection fallback
    MethodInfo = canUseAot ? null : GetMethodInfo()
});
```

## Service Registration

### Old Architecture
```csharp
// Old: Many services with unclear responsibilities
services.Register<ITestsConstructor>(new BaseTestsConstructor(...));
services.Register<ITestsCollector>(new TestsCollector(...));
services.Register<ITestBuilder>(new StaticTestBuilder(...));
services.Register<ITestBuilder>(new DynamicTestBuilder(...));
services.Register<ITestVariationBuilder>(new TestVariationBuilder(...));
// ... many more
```

### New Architecture
```csharp
// New: Few services with clear responsibilities
services.Register<TestDiscoveryService>(...);
services.Register<TestFactory>(...);
services.Register<UnifiedTestExecutor>(...);
```

## Benefits Summary

1. **Fewer Classes**: ~50% reduction in core classes
2. **Clear Flow**: TestMetadata → TestFactory → ExecutableTest → Execution
3. **Built-in AOT**: No wrapper classes needed
4. **Single Path**: One way to do things instead of multiple
5. **Better Performance**: Fewer allocations and transformations