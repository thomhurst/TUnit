using TUnit.Core;

namespace TUnit.TestAdapter;

internal record TestWithResult(TestWithTestCase Test, Task<TUnitTestResultWithDetails> Result);