using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;

namespace TUnit.TestAdapter;

internal record TestWithResult(TestCase Test, Task<TUnitTestResult> Result);