# Discovery Answers

## Q1: Will this unified test builder need to replace multiple existing builder implementations?
**Answer:** Yes
- Two data collection mechanisms: reflection-based and source generator-based
- Unified test builder consumes data from either mechanism
- Breaking changes are acceptable to achieve optimal design

## Q2: Should the unified test builder maintain backward compatibility with existing test metadata structures?
**Answer:** No
- Design optimal structure without compatibility constraints
- Can make breaking changes and remove old structures
- Must consider: generic test classes, generic test methods, various data injection sources
- Support data injection into: methods, constructors, and properties (single values only)

## Q3: Will the unified test builder need to support both AOT and reflection-based test creation?
**Answer:** Yes
- Source generation mode (default): fully AOT compatible, no warnings suppressed
- Reflection mode: fallback for F#, VB.NET, etc., not expected to be AOT compatible

## Q4: Does the unified test builder need to handle data-driven test expansion?
**Answer:** Yes
- Support all data injection mechanisms:
  - Arguments attribute
  - MethodDataSource attribute
  - Custom generators inheriting from AsyncDataSourceGeneratorAttribute
- Support injection into: class constructors, method arguments, and properties
- This is a major unique selling point of TUnit

## Q5: Should the unified test builder consolidate test creation logic that's currently spread across multiple components?
**Answer:** Yes
- Design for cleanest architecture
- Should be: simple, easy to extend, easy to maintain, easy to understand