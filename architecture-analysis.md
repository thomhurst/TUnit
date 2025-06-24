# TUnit Architecture Analysis - Over-Engineering Assessment

## Current Architecture Flow

### 1. Compile-Time (Source Generation)
```
[Test] Method → TestMetadataGenerator → StaticTestDefinition/DynamicTestMetadata
                                          ↓
                                    SourceRegistrar.Register(TestMetadataSource)
```

### 2. Runtime Discovery Pipeline
```
TUnitTestDiscoverer → BaseTestsConstructor → TestsCollector → TestMetadataSource
         ↓                    ↓                    ↓              ↓
   GetTestsAsync()    DiscoverTestsAsync()  DiscoverTestsAsync()  BuildTestDefinitionsAsync()
         ↓                    ↓                    ↓              ↓
   FilterTests()      DependencyCollector    Sources.TestSources  StaticTestBuilder/DynamicTestBuilder
```

### 3. Test Building Pipeline
```
TestMetadataSource → ITestDefinitionBuilder → TestDefinition → TestBuilder → DiscoveredTest
                           ↓                        ↓              ↓
                    StaticTestBuilder         TestDefinition   BuildTests()
                    DynamicTestBuilder        TestDefinition<T>    ↓
                                                              UnifiedDiscoveredTest
                                                              DiscoveredTest<T>
```

### 4. Execution Pipeline
```
TestsExecutor → SingleTestExecutor → TestInvoker → Test Method
```

## Identified Redundancies

### 1. Multiple "Constructor" Concepts
- **BaseTestsConstructor** - Abstract base for test construction
- **SourceGeneratedTestsConstructor** - Concrete implementation for source-generated tests
- **ReflectionTestsConstructor** - For reflection-based discovery
- **TestBuilder** (in Engine) - Actually builds DiscoveredTest from TestDefinition
- **StaticTestBuilder/DynamicTestBuilder** (in Core) - Build TestDefinition from metadata

**Problem**: Too many layers doing similar transformations.

### 2. Multiple "Builder" Classes
- **ITestDefinitionBuilder** interface with StaticTestBuilder/DynamicTestBuilder
- **TestBuilder** in Engine that builds DiscoveredTest
- **ReflectionTestConstructionBuilder** for reflection scenarios
- **TestMetadataGenerator** that "builds" metadata

**Problem**: Confusing naming and overlapping responsibilities.

### 3. AOT Wrapper Complexity
- **TestDefinition** (non-generic) - Base definition
- **TestDefinition<T>** (generic) - AOT-safe typed version
- **StaticTestDefinition** - Source-generated AOT metadata
- **DynamicTestMetadata** - For reflection-based tests
- Conversion between them involves multiple implicit operators

**Problem**: Too many types representing essentially the same concept.

### 4. Discovery vs Building Confusion
- **TUnitTestDiscoverer** - Doesn't actually discover, delegates to constructor
- **BaseTestsConstructor** - Doesn't construct tests, delegates to TestsCollector
- **TestsCollector** - Doesn't collect tests, dequeues from Sources
- **TestMetadataSource** - Actually discovers by building definitions

**Problem**: Class names don't match their actual responsibilities.

### 5. Data Flow Redundancy
```
TestMetadata → TestDefinition → DiscoveredTest → Execution
```
Each transformation adds little value but requires separate classes and interfaces.

## Simplification Opportunities

### 1. Collapse Discovery Chain
Replace:
```
TUnitTestDiscoverer → BaseTestsConstructor → TestsCollector → TestMetadataSource
```

With:
```
TestDiscoveryService → TestMetadataSource
```

### 2. Unify Builders
Replace multiple builders with a single TestFactory:
```
TestMetadata → TestFactory → DiscoveredTest
```

### 3. Simplify AOT Types
Use a single TestMetadata type with optional typed factory methods:
```
TestMetadata {
  // Common properties
  Func<object>? UntypedFactory
  object? TypedFactory // Cast when needed
}
```

### 4. Remove Intermediate Types
- Eliminate TestDefinition - go directly from metadata to DiscoveredTest
- Remove ITestDefinitionBuilder hierarchy
- Combine TestsCollector functionality into discovery service

### 5. Clear Naming
- TestDiscoveryService - Actually discovers tests
- TestFactory - Creates executable tests from metadata
- TestMetadata - Compile-time test information
- DiscoveredTest - Runtime executable test

## Proposed Simplified Architecture

### Compile-Time
```
[Test] → SourceGenerator → TestMetadata → Register
```

### Runtime
```
TestDiscoveryService
    ↓
Read TestMetadata from Sources
    ↓
TestFactory.CreateTests(metadata) → DiscoveredTest[]
    ↓
TestExecutor.Execute(discoveredTest)
```

### Benefits
1. Fewer layers and indirections
2. Clear responsibilities for each component
3. Simpler AOT story - one metadata type, typed factories
4. Easier to understand and maintain
5. Less code overall

## Action Items
1. Rename classes to match actual responsibilities
2. Collapse redundant transformation layers
3. Unify builder pattern into single factory
4. Simplify AOT type hierarchy
5. Remove unnecessary abstractions