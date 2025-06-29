# Initial Request

**Timestamp:** 2025-06-29 19:12
**Request:** Fix the existing AOT errors in any source-generated code paths. We seem to be creating DynamicTestDataSource in source generated tests which may not be the correct approach since it relies on reflection APIs

## Context
The user has identified that the current source generation approach is creating `DynamicTestDataSource` instances that require reflection APIs, which is incompatible with AOT (Ahead-of-Time) compilation. This is causing build warnings/errors in the source-generated code paths.

## Key Problem Areas Identified
- Source generators creating `DynamicTestDataSource` instances
- These instances rely on reflection APIs
- This conflicts with AOT compilation requirements
- Need to find alternative approach for AOT-compatible test data sources