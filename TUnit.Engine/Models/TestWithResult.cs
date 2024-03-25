using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

namespace TUnit.Engine.Models;

internal record TestWithResult(TestNode Test, Task<TUnitTestResult> ResultTask);