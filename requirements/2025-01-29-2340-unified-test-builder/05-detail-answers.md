# Expert Requirements Answers

## Q6: Should the unified test builder use a pipeline pattern with distinct stages for data collection, expansion, and test creation?
**Answer:** Yes
- Pipeline stages: Data Collection → Data Expansion → Test Creation
- Each stage has single responsibility
- Makes the flow easier to understand and maintain

## Q7: Should we create separate data collectors for AOT (reading from generated metadata) and reflection (runtime discovery)?
**Answer:** Yes
- **AOT/Source Generation approach**:
  - Arguments: Write constant values directly (known at compile time)
  - Method data sources: Write strongly typed factories/delegates to invoke during data collection
  - AsyncDataSourceGenerator attributes: Instantiate and call generate method
- **Reflection approach**:
  - Scan for attributes
  - All should implement common interface with `GenerateData()` method
  - Returns `IEnumerable<object?[]>`

## Q8: Should property injection be treated as a first-class data source rather than a post-processing step?
**Answer:** Yes
- Reuse existing logic for expanding class and method level data sources
- If enumerable is returned, filter to first item only
- More consistent with other data injection mechanisms

## Q9: Should generic type resolution be handled in a separate pre-processing stage before test expansion?
**Answer:** Yes
- Simplifies main builder logic
- Makes generic handling more testable
- Cleaner separation of concerns

## Q10: Should we implement a caching layer for resolved data sources to improve performance of shared data sources?
**Answer:** No
- Each test should have separate object instances
- Prevents state leakage between tests
- Keeps tests isolated and side-effect free

## Q10a: Should data sources return factory functions (Func<object?[]>) instead of materialized data, allowing each test to get fresh instances?
**Answer:** Yes
- Data sources return `IEnumerable<Func<object?[]>>` instead of `IEnumerable<object?[]>`
- Each test invokes the factory to get fresh instances
- Ensures no shared state between tests
- Clean API while maintaining isolation