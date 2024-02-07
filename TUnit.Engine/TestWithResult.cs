using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;

namespace TUnit.Engine;

internal record TestWithResult(TestCase Test, Task<TUnitTestResult> Result);