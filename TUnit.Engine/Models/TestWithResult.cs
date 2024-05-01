using TUnit.Core;

namespace TUnit.Engine.Models;

internal record TestWithResult(TestInformation Test, Task<TUnitTestResult> ResultTask);