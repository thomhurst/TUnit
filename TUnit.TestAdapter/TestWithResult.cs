using TUnit.Core;

namespace TUnit.TestAdapter;

public record TestWithResult(TestWithTestCase Test, Task<TUnitTestResult> Result);