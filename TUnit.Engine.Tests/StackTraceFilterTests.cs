using Shouldly;
using TUnit.Engine.Exceptions;

namespace TUnit.Engine.Tests;

public class StackTraceFilterTests
{
    [Test]
    public void UserFrames_BeforeTUnitFrames_ArePreserved()
    {
        var stackTrace = string.Join(Environment.NewLine,
            "   at MyApp.UserService.GetUser(Int32 id) in C:\\src\\UserService.cs:line 42",
            "   at MyApp.Tests.UserTests.TestGetUser() in C:\\src\\Tests.cs:line 15",
            "   at TUnit.Core.RunHelpers.RunAsync()",
            "   at TUnit.Engine.TestExecutor.ExecuteAsync()");

        var result = TUnitFailedException.FilterStackTrace(stackTrace);

        result.ShouldContain("MyApp.UserService.GetUser");
        result.ShouldContain("MyApp.Tests.UserTests.TestGetUser");
        result.ShouldNotContain("TUnit.Core.RunHelpers");
        result.ShouldNotContain("TUnit.Engine.TestExecutor");
    }

    [Test]
    public void UserFrames_AfterTUnitFrames_ArePreserved()
    {
        // Assertion internals at top, user test method below
        var stackTrace = string.Join(Environment.NewLine,
            "   at TUnit.Assertions.AssertCondition.Check()",
            "   at TUnit.Assertions.AssertionBuilder.Process()",
            "   at MyApp.Tests.UserTests.TestGetUser() in C:\\src\\Tests.cs:line 15",
            "   at TUnit.Core.RunHelpers.RunAsync()");

        var result = TUnitFailedException.FilterStackTrace(stackTrace);

        result.ShouldContain("MyApp.Tests.UserTests.TestGetUser");
        result.ShouldNotContain("TUnit.Assertions.AssertCondition");
        result.ShouldNotContain("TUnit.Core.RunHelpers");
    }

    [Test]
    public void AllTUnitFrames_PreservesFullTrace()
    {
        // TUnit bug — no user frames at all
        var stackTrace = string.Join(Environment.NewLine,
            "   at TUnit.Engine.SomeService.Process()",
            "   at TUnit.Engine.TestExecutor.ExecuteAsync()");

        var result = TUnitFailedException.FilterStackTrace(stackTrace);

        result.ShouldBe(stackTrace);
    }

    [Test]
    public void EmptyStackTrace_ReturnsEmpty()
    {
        TUnitFailedException.FilterStackTrace(null).ShouldBeEmpty();
        TUnitFailedException.FilterStackTrace("").ShouldBeEmpty();
    }

    [Test]
    public void OmittedFrames_ShowHint()
    {
        var stackTrace = string.Join(Environment.NewLine,
            "   at MyApp.Tests.UserTests.TestGetUser() in C:\\src\\Tests.cs:line 15",
            "   at TUnit.Core.RunHelpers.RunAsync()");

        var result = TUnitFailedException.FilterStackTrace(stackTrace);

        result.ShouldContain("--detailed-stacktrace");
    }

    [Test]
    public void NoTUnitFrames_NoHint()
    {
        var stackTrace = string.Join(Environment.NewLine,
            "   at MyApp.UserService.GetUser(Int32 id) in C:\\src\\UserService.cs:line 42",
            "   at MyApp.Tests.UserTests.TestGetUser() in C:\\src\\Tests.cs:line 15");

        var result = TUnitFailedException.FilterStackTrace(stackTrace);

        result.ShouldNotContain("--detailed-stacktrace");
        result.ShouldBe(stackTrace);
    }

    [Test]
    public void TUnitWithoutDot_IsNotFiltered()
    {
        // "TUnitExtensions" is NOT a TUnit internal namespace
        var stackTrace = string.Join(Environment.NewLine,
            "   at TUnitExtensions.MyHelper.DoSomething()",
            "   at TUnit.Core.RunHelpers.RunAsync()");

        var result = TUnitFailedException.FilterStackTrace(stackTrace);

        result.ShouldContain("TUnitExtensions.MyHelper");
        result.ShouldNotContain("TUnit.Core.RunHelpers");
    }

    [Test]
    public void InterleavedFrames_PreservesAllUserFrames()
    {
        var stackTrace = string.Join(Environment.NewLine,
            "   at MyApp.Database.Connect() in C:\\src\\Database.cs:line 10",
            "   at TUnit.Core.SomeHelper.DoSomething()",
            "   at MyApp.Tests.TestBase.Setup() in C:\\src\\TestBase.cs:line 5",
            "   at TUnit.Engine.TestExecutor.ExecuteAsync()");

        var result = TUnitFailedException.FilterStackTrace(stackTrace);

        result.ShouldContain("MyApp.Database.Connect");
        result.ShouldContain("MyApp.Tests.TestBase.Setup");
        result.ShouldNotContain("TUnit.Core.SomeHelper");
        result.ShouldNotContain("TUnit.Engine.TestExecutor");
    }

    [Test]
    public void AsyncInfrastructureLines_ArePreserved()
    {
        var stackTrace = string.Join(Environment.NewLine,
            "   at MyApp.UserService.GetUser(Int32 id) in C:\\src\\UserService.cs:line 42",
            "--- End of stack trace from previous location ---",
            "   at MyApp.Tests.UserTests.TestGetUser() in C:\\src\\Tests.cs:line 15",
            "   at TUnit.Core.RunHelpers.RunAsync()");

        var result = TUnitFailedException.FilterStackTrace(stackTrace);

        result.ShouldContain("End of stack trace from previous location");
        result.ShouldContain("MyApp.UserService.GetUser");
        result.ShouldContain("MyApp.Tests.UserTests.TestGetUser");
    }
}
