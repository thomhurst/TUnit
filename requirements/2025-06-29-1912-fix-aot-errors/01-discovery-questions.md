# Discovery Questions

## Q1: Should we maintain backward compatibility with existing test code using data source attributes?
**Default if unknown:** Yes (breaking changes to test attributes would require major migration effort)

## Q2: Can static method data sources be resolved at compile time for AOT compatibility?
**Default if unknown:** Yes (static methods with constant return values can be evaluated during compilation)

## Q3: Should dynamic data sources be completely disabled in AOT mode?
**Default if unknown:** No (some runtime data may be necessary, but should have AOT alternatives)

## Q4: Will this fix need to work with all existing data source attribute types (MethodDataSource, ClassDataSource, PropertyDataSource)?
**Default if unknown:** Yes (comprehensive fix should handle all data source types consistently)

## Q5: Should the source generator pre-evaluate static data sources to avoid runtime reflection?
**Default if unknown:** Yes (compile-time evaluation is the core principle of AOT compatibility)