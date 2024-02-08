using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;

namespace TUnit.Engine.Models;

internal record TestWithResult(TestCase Test, Task<TUnitTestResult> ResultTask);