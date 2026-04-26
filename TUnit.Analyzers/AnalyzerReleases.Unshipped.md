### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TUnit0061 | Usage | Error | ClassDataSource type requires parameterless constructor
TUnit0062 | Usage | Warning | CancellationToken must be the last parameter
TUnit0073 | Usage | Error | Missing polyfill types required by TUnit
TUnit0074 | Usage | Error | Hook attribute is redundant on an override

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TUnit0015 | Usage | Error | Changed to Warning severity (CancellationToken parameter now optional)
TUnit0043 | Usage | Error | Changed to Info severity (now a suggestion instead of error)
TUnit0300 | Usage | Warning | Removed - rule was inaccurate and produced false positives
TUnit0301 | Usage | Warning | Removed - rule was inaccurate and produced false positives
TUnit0302 | Usage | Warning | Removed - rule was never implemented and the underlying claim was inaccurate