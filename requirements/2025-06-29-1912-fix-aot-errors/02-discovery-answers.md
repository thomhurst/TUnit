# Discovery Answers

## Q1: Should we maintain backward compatibility with existing test code using data source attributes?
**Answer:** Yes
**Details:** We should adjust the source generation and/or the engine logic. The attributes that test authors use should stay the same.

## Q2: Can static method data sources be resolved at compile time for AOT compatibility?
**Answer:** Yes
**Details:** We should resolve whatever we can during compilation to ensure strong-typing and not relying on reflection.

## Q3: Should dynamic data sources be completely disabled in AOT mode?
**Answer:** No
**Details:** For most data source types, we have all the information available at compile time. However, for true dynamic sources (like AsyncUntypedDataSourceGeneratorAttribute) we may need to add a `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]` attribute so that when test authors use them, they will get appropriate warnings (unless you can think of a better solution to this?)

## Q4: Will this fix need to work with all existing data source attribute types (MethodDataSource, ClassDataSource, PropertyDataSource)?
**Answer:** Yes
**Details:** The appropriate types are: MethodDataSource, Arguments, and AsyncDataSourceGeneratorAttribute (and all that inherit from it).

## Q5: Should the source generator pre-evaluate static data sources to avoid runtime reflection?
**Answer:** Yes
**Details:** Confirmed - compile-time evaluation is the core principle of AOT compatibility.