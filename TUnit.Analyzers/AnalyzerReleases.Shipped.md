## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUnit0001 | Usage | Error | Test argument types don't match method parameters
TUnit0002 | Usage | Error | No data provided for test
TUnit0004 | Usage | Error | Data source method not found
TUnit0005 | Usage | Warning | Nullable parameter has non-nullable argument - consider making the argument nullable
TUnit0007 | Usage | Error | Data source method must be static
TUnit0008 | Usage | Error | Data source method must be public
TUnit0009 | Usage | Error | Data source method cannot be abstract
TUnit0010 | Usage | Error | Data source method must be parameterless
TUnit0011 | Usage | Error | Data source method must return data (IEnumerable or Task<IEnumerable>)
TUnit0013 | Usage | Error | Test has more arguments than method parameters
TUnit0014 | Usage | Warning | Public test method missing [Test] attribute - add attribute or make method private/protected
TUnit0015 | Usage | Error | Methods with [Timeout] must have a CancellationToken parameter
TUnit0016 | Usage | Error | Hook methods must not be static
TUnit0017 | Usage | Error | [Explicit] attribute cannot be on both method and class - choose one location
TUnit0018 | Usage | Warning | Test methods should not assign instance fields/properties - consider using static fields or [NotInParallel]
TUnit0019 | Usage | Error | Test method missing [Test] attribute
TUnit0023 | Usage | Warning | Disposable fields/properties should be disposed in cleanup methods ([After(Test)] or [After(Class)])
TUnit0027 | Usage | Error | Hook method has incorrect parameters - check expected parameter types for this hook
TUnit0028 | Usage | Error | Do not override TUnit's AttributeUsage settings
TUnit0029 | Usage | Error | Duplicate attribute where only one is allowed
TUnit0030 | Usage | Warning | Test class doesn't inherit base class tests - add [InheritsTests] to include them
TUnit0031 | Usage | Error | Async void methods not allowed - return Task instead
TUnit0032 | Usage | Error | [DependsOn] and [NotInParallel] attributes conflict - tests with dependencies must support parallel execution
TUnit0033 | Usage | Error | Circular or conflicting test dependencies detected
TUnit0034 | Usage | Error | Do not declare a Main method in test projects - TUnit provides its own entry point
TUnit0038 | Usage | Error | Property with data attribute must have a data source attribute
TUnit0039 | Usage | Error | Test hook methods require single TestContext parameter
TUnit0040 | Usage | Error | Class hook methods require single ClassHookContext parameter
TUnit0041 | Usage | Error | Assembly hook methods require single AssemblyHookContext parameter
TUnit0042 | Usage | Warning | Global hooks should be in separate classes from tests for clarity
TUnit0043 | Usage | Error | Properties with data attributes must use 'required' keyword
TUnit0044 | Usage | Error | Properties with data attributes must have a setter
TUnit0045 | Usage | Error | Property has multiple data source attributes - use only one
TUnit0046 | Usage | Warning | Data source should return Func<T> for lazy evaluation instead of T
TUnit0047 | Usage | Warning | AsyncLocal values from BeforeTest hooks require context.AddAsyncLocalValue() to flow to tests
TUnit0048 | Usage | Error | Test methods in non-static classes must not be static
TUnit0049 | Usage | Error | [Matrix] parameters require [MatrixDataSource] attribute on the test method
TUnit0050 | Usage | Error | Too many test arguments provided
TUnit0051 | Usage | Error | Test class must be public
TUnit0055 | Usage | Warning | Do not overwrite Console.Out/Error - it breaks TUnit logging
TUnit0056 | Usage | Error | Instance data source methods must use [InstanceMethodDataSource] attribute
TUnit0058 | Usage | Error | Generic test methods require [GenerateGenericTest] for AOT compatibility
TUnit0059 | Usage | Error | Dynamic data sources using reflection are not AOT-compatible - use static sources
TUnit0060 | Usage | Error | Open generic types are not AOT-compatible - specify concrete type arguments
TUnit0200 | Usage | Warning | Avoid blocking on async code (.Result, .GetAwaiter().GetResult()) - use await instead
TUXU0001 | Usage | Info | XUnit code can be migrated to TUnit