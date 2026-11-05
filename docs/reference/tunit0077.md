# TUnit0077: Span params parameters are not supported on tests

**Severity:** Error

TUnit reports this diagnostic when a test method declares a `params ReadOnlySpan<T>` or `params Span<T>` parameter, including generic test methods fed by typed data rows.

Spans cannot be boxed into the argument array used by reflection invocation. TUnit therefore does not support span params parameters in either execution mode.

Change the parameter to `params T[]`. For example, replace `params ReadOnlySpan<string> rest` with `params string[] rest`. Array params support packing the trailing elements of a typed data row in both source-generated and reflection modes.

The rule applies to test methods regardless of their data source. Ordinary helper methods can still use span params.
