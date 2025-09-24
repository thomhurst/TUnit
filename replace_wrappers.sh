#!/bin/bash

# Replace all wrapper class references with fluent assertion classes
find TUnit.Assertions/Assertions -name "*.cs" -type f -exec sed -i \
  -e 's/BetweenAssertionBuilderWrapper/BetweenAssertion/g' \
  -e 's/NotBetweenAssertionBuilderWrapper/NotBetweenAssertion/g' \
  -e 's/EquivalentToAssertionBuilderWrapper/EquivalentToAssertion/g' \
  -e 's/NotEquivalentToAssertionBuilderWrapper/NotEquivalentToAssertion/g' \
  -e 's/ParseAssertionBuilderWrapper/ParseAssertion/g' \
  -e 's/SingleItemAssertionBuilderWrapper/SingleItemAssertion/g' \
  -e 's/DateOnlyEqualToAssertionBuilderWrapper/DateOnlyEqualToAssertion/g' \
  -e 's/DateTimeOffsetEqualToAssertionBuilderWrapper/DateTimeOffsetEqualToAssertion/g' \
  -e 's/TimeOnlyEqualToAssertionBuilderWrapper/TimeOnlyEqualToAssertion/g' \
  -e 's/TimeSpanEqualToAssertionBuilderWrapper/TimeSpanEqualToAssertion/g' \
  {} \;

echo "Replacements complete"
