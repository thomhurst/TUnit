# TUnit0076: Data source Arguments fit more than one overload equally well

**Severity:** Error

The `Arguments` of a `[MethodDataSource]` select which overload of the data source method is called. TUnit0076 reports `Arguments` that fit two or more overloads equally well, where no overload is more specific than the others. A C# call with the same arguments usually fails with CS0121 for the same reason.

For example, with the overloads `Tie(string expected, IComparable value)` and `Tie(string expected, IConvertible value)`, the attribute `[MethodDataSource(nameof(Tie), Arguments = new object[] { "a", 1 })]` is ambiguous: `int` implements both interfaces, and neither interface converts to the other.

## Why it is an error

Without the diagnostic, the two execution modes disagree. Reflection mode asks the runtime binder for the overload that matches the argument types, and the binder throws `AmbiguousMatchException`. Source-generated mode would call one of the overloads instead.

## How to fix it

Make one overload the best match for the `Arguments`:

- Rename one of the overloads, so the attribute names the method it means.
- Change a parameter type so that one overload is more specific, for example `int` instead of `IConvertible`.
- Remove the overload that the data source does not need.

## Scope

The rule follows the runtime binder's rules, which differ from C# overload resolution in a few places:

- It compares only overloads with one parameter per argument, and skips overloads with `ref`, `in` or `out` parameters, parameters typed by a generic method's type parameter, and private methods declared on a base class, because the runtime lookup never selects them.
- It accepts identity, reference and boxing conversions, `T` to `Nullable<T>`, and the runtime's primitive widening conversions. These are narrower than C#'s implicit numeric conversions: `int` does not widen to `decimal`, `nint` or `nuint`. They also allow `byte` to `char`, which C# does not.
- It does not check `Arguments` that contain `null`, or enum arguments passed to primitive parameters.
