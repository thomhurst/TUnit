# Discovery Questions

The following yes/no questions will help understand the requirements for a unified test builder approach in TUnit:

## Q1: Will this unified test builder need to replace multiple existing builder implementations?
**Default if unknown:** Yes (based on seeing multiple builder-related classes in the codebase)

## Q2: Should the unified test builder maintain backward compatibility with existing test metadata structures?
**Default if unknown:** Yes (breaking changes would impact all existing tests)

## Q3: Will the unified test builder need to support both AOT and reflection-based test creation?
**Default if unknown:** Yes (TUnit emphasizes AOT support while maintaining reflection compatibility)

## Q4: Does the unified test builder need to handle data-driven test expansion?
**Default if unknown:** Yes (current TestFactory already handles this)

## Q5: Should the unified test builder consolidate test creation logic that's currently spread across multiple components?
**Default if unknown:** Yes (cleaner architecture with single responsibility)