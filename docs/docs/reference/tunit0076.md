# TUnit0076: Data source Arguments fit more than one overload equally well

**Severity:** Error

The `Arguments` of a `[MethodDataSource]` select which overload of the data source method is called. TUnit0076 reports `Arguments` that fit two or more overloads equally well, where no overload is more specific than the others. A C# call with the same arguments would fail with CS0121 for the same reason.

For example, with the overloads `Tie(string expected, IComparable value)` and `Tie(string expected, IConvertible value)`, the attribute `[MethodDataSource(nameof(Tie), Arguments = new object[] { "a", 1 })]` is ambiguous: `int` implements both interfaces, and neither interface converts to the other.

## Why it is an error

Without the diagnostic, the two execution modes disagree. Reflection mode asks the runtime binder for the overload that matches the argument types, and the binder throws `AmbiguousMatchException`. Source-generated mode would call one of the overloads instead.

## How to fix it

Make one overload the best match for the `Arguments`:

- Rename one of the overloads, so the attribute names the method it means.
- Change a parameter type so that one overload is more specific, for example `int` instead of `IConvertible`.
- Remove the overload that the data source does not need.

## Scope

The rule follows the runtime binder's rules. It compares only overloads with one parameter per argument, and it accepts identity, reference, boxing and numeric widening conversions. It does not check `Arguments` that contain `null` or overloads that are generic methods.
