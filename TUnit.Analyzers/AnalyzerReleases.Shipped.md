## Release 1.0

### New Rules

#### Test Method and Structure Rules
Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUnit0001 | Usage | Error | Test argument types don't match method parameters
TUnit0002 | Usage | Error | No data provided for test
TUnit0013 | Usage | Error | Test has more arguments than method parameters
TUnit0014 | Usage | Warning | Public test method missing [Test] attribute - add attribute or make method private/protected
TUnit0019 | Usage | Error | Test method missing [Test] attribute
TUnit0048 | Usage | Error | Test methods in non-static classes must not be static
TUnit0051 | Usage | Error | Test class must be public
TUnit0052 | Usage | Warning | Multiple constructors found without [TestConstructor] attribute

#### Data Source and Parameter Rules
Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUnit0004 | Usage | Error | Data source method not found
TUnit0005 | Usage | Warning | Nullable parameter has non-nullable argument - consider making the argument nullable
TUnit0007 | Usage | Error | Data source method must be static
TUnit0008 | Usage | Error | Data source method must be public
TUnit0009 | Usage | Error | Data source method cannot be abstract
TUnit0010 | Usage | Error | Data source method must be parameterless
TUnit0011 | Usage | Error | Data source method must return data (IEnumerable or Task<IEnumerable>)
TUnit0038 | Usage | Error | Property with data attribute must have a data source attribute
TUnit0043 | Usage | Error | Properties with data attributes must use 'required' keyword
TUnit0044 | Usage | Error | Properties with data attributes must have a setter
TUnit0045 | Usage | Error | Property has multiple data source attributes - use only one
TUnit0046 | Usage | Warning | Data source should return Func<T> for lazy evaluation instead of T
TUnit0049 | Usage | Error | [Matrix] parameters require [MatrixDataSource] attribute on the test method
TUnit0050 | Usage | Error | Too many test arguments provided
TUnit0056 | Usage | Error | Instance data source methods must use [InstanceMethodDataSource] attribute

#### Hook and Lifecycle Rules
Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUnit0016 | Usage | Error | Hook methods must not be static
TUnit0027 | Usage | Error | Hook method has incorrect parameters - check expected parameter types for this hook
TUnit0039 | Usage | Error | Test hook methods require single TestContext parameter
TUnit0040 | Usage | Error | Class hook methods require single ClassHookContext parameter
TUnit0041 | Usage | Error | Assembly hook methods require single AssemblyHookContext parameter
TUnit0042 | Usage | Warning | Global hooks should be in separate classes from tests for clarity
TUnit0047 | Usage | Warning | AsyncLocal values from BeforeTest hooks require context.AddAsyncLocalValue() to flow to tests
TUnit0057 | Usage | Info | Hook context parameter available - consider adding for additional context information
TUnit0058 | Usage | Error | Hook method has unknown parameters - check expected parameter types for this hook

#### Attribute and Metadata Rules
Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUnit0017 | Usage | Error | [Explicit] attribute cannot be on both method and class - choose one location
TUnit0028 | Usage | Error | Do not override TUnit's AttributeUsage settings
TUnit0029 | Usage | Error | Duplicate attribute where only one is allowed
TUnit0030 | Usage | Warning | Test class doesn't inherit base class tests - add [InheritsTests] to include them
TUnit0032 | Usage | Error | [DependsOn] and [NotInParallel] attributes conflict - tests with dependencies must support parallel execution
TUnit0033 | Usage | Error | Circular or conflicting test dependencies detected

#### Async and Execution Rules
Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUnit0015 | Usage | Error | Methods with [Timeout] must have a CancellationToken parameter
TUnit0031 | Usage | Error | Async void methods not allowed - return Task instead

#### AOT Compatibility Rules
Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUnit0300 | Usage | Warning | Generic types may not be AOT-compatible - ensure all combinations are known at compile time
TUnit0301 | Usage | Warning | Tuple usage may not be AOT-compatible - consider using concrete types
TUnit0302 | Usage | Warning | Custom conversion operators may not be AOT-compatible - use explicit casting

#### Best Practices and Warnings
Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUnit0018 | Usage | Warning | Test methods should not assign instance fields/properties - consider using static fields or [NotInParallel]
TUnit0023 | Usage | Warning | Disposable fields/properties should be disposed in cleanup methods ([After(Test)] or [After(Class)])
TUnit0034 | Usage | Error | Do not declare a Main method in test projects - TUnit provides its own entry point
TUnit0055 | Usage | Warning | Do not overwrite Console.Out/Error - it breaks TUnit logging

#### Migration and Legacy Support
Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUXU0001 | Usage | Info | XUnit code can be migrated to TUnit