# Suggested Analyzer Message Improvements

This document lists suggested improvements for analyzer messages that could be clearer to users.

## TUnit.Analyzers Resource String Improvements

### TUnit0004
**Current Title:** "No method found"
**Suggested Title:** "Data source method not found"
**Reason:** Provides context that this is about data source methods specifically

### TUnit0005  
**Current Title:** "The parameter is not defined as nullable"
**Suggested Title:** "Nullable parameter has non-nullable argument"
**Current Description:** "The parameter is not defined as nullable."
**Suggested Description:** "The test parameter '{0}' is nullable but the provided argument is not. Consider making the argument nullable to match."

### TUnit0010
**Current Title:** "Method should be parameterless"
**Suggested Title:** "Data source method must be parameterless"
**Reason:** Clarifies this applies to data source methods

### TUnit0011
**Current Title:** "Method returns void"
**Suggested Title:** "Data source method must return data"
**Current Description:** "Method returns void."
**Suggested Description:** "Data source methods must return IEnumerable<T> or Task<IEnumerable<T>>, not void."

### TUnit0013
**Current Title:** "A data source method must only have 1 matching parameter"
**Suggested Title:** "Test has more arguments than parameters"
**Current Description:** "A data source method must only have 1 matching parameter."
**Suggested Description:** "The test method has {0} parameters but {1} arguments were provided."

### TUnit0027
**Current Title:** "Unknown Parameters"
**Suggested Title:** "Hook method has incorrect parameters"
**Current MessageFormat:** "Method parameters should be {0}"
**Suggested MessageFormat:** "This {0} hook method must have parameters: {1}"

### TUnit0039-0041
**Current Titles:** "Single parameter of `XContext` required"
**Suggested Titles:** 
- TUnit0039: "Test hooks require TestContext parameter"
- TUnit0040: "Class hooks require ClassHookContext parameter"  
- TUnit0041: "Assembly hooks require AssemblyHookContext parameter"

### TUnit0043
**Current Title:** "Property must use `required` keyword"
**Suggested Title:** "Data property must use 'required' keyword"
**Suggested Description:** "Properties with data source attributes must be marked as 'required' to ensure they are initialized."

### TUnit0046
**Current Title:** "Return a `Func<T>` rather than a `<T>`"
**Suggested Title:** "Data source should return Func<T> for lazy evaluation"
**Suggested Description:** "Data source methods should return Func<T> instead of T to enable lazy evaluation and avoid shared state."

## TUnit.Assertions.Analyzers Resource String Improvements

### TUnitAssertions0003
**Current Title:** "Compiler argument populated"
**Suggested Title:** "Don't provide CallerArgumentExpression values"
**Current Description:** "Do not populate this argument. The compiler will do this."
**Suggested Description:** "The CallerArgumentExpression parameter will be automatically provided by the compiler. Remove the explicit argument."

### TUnitAssertions0005
**Current Description:** "Assert.That(...) should not be used with a constant value."
**Suggested Description:** "Assert.That(...) with a constant value will always pass or fail. Did you mean to test a variable instead?"

### TUnitAssertions0007
**Current Title:** "Cast dynamic values to `object?` when using Assert.That(...)"
**Suggested Description:** "Dynamic values must be cast to 'object?' for proper type inference in Assert.That(). Example: Assert.That((object?)dynamicValue)"

These improvements aim to:
1. Add context about which type of method/attribute the rule applies to
2. Explain why the rule exists and what the user should do
3. Use more specific terminology that matches TUnit concepts
4. Provide examples where helpful