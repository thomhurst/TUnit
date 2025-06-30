# Expert Requirements Questions

Based on my analysis of the codebase, here are the most important detailed questions:

## Q6: Should the unified test builder use a pipeline pattern with distinct stages for data collection, expansion, and test creation?
**Default if unknown:** Yes (promotes single responsibility and makes the flow easier to understand)

## Q7: Should we create separate data collectors for AOT (reading from generated metadata) and reflection (runtime discovery)?
**Default if unknown:** Yes (cleaner separation of concerns, easier to maintain feature parity)

## Q8: Should property injection be treated as a first-class data source rather than a post-processing step?
**Default if unknown:** Yes (more consistent with method and constructor argument handling)

## Q9: Should generic type resolution be handled in a separate pre-processing stage before test expansion?
**Default if unknown:** Yes (simplifies the main builder logic and makes generic handling more testable)

## Q10: Should we implement a caching layer for resolved data sources to improve performance of shared data sources?
**Default if unknown:** Yes (the current DataSourceResolver mentions caching but doesn't implement it)