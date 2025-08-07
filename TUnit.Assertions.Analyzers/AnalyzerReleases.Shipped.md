## Release 1.0

### New Rules

#### Assertion Usage Rules
Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUnitAssertions0001 | Usage | Warning | Don't mix 'Or' & 'And' operators in assertions - use parentheses to clarify precedence
TUnitAssertions0002 | Usage | Error | Assert statements must be awaited - all TUnit assertions return Task
TUnitAssertions0004 | Usage | Error | Assert.Multiple requires 'using' statement for proper scoping

#### Assertion Best Practices
Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUnitAssertions0003 | Usage | Warning | Don't provide CallerArgumentExpression parameter values - let the compiler handle it
TUnitAssertions0005 | Usage | Warning | Assert.That() should not be used with constant values - the assertion will always pass or fail
TUnitAssertions0006 | Usage | Error | Use .IsEqualTo() instead of calling object.Equals() for better assertion messages
TUnitAssertions0007 | Usage | Error | Cast dynamic values to 'object?' when using Assert.That() for proper type inference
TUnitAssertions0008 | Usage | Error | Await ValueTask before passing to Assert.That() - use 'await' keyword

#### Migration Support
Rule ID | Category | Severity | Notes                                          
--------|----------|----------|------------------------------------------------
TUnitAssertions0009 | Usage | Info | XUnit assertion can be migrated to TUnit assertion syntax