using TUnit.Core;
using TUnit.Engine.Json;

namespace TUnit.Engine.Extensions;

internal static class JsonExtensions
{
    public static TestSessionJson ToJsonModel(this TestSessionContext context)
    {
        return new TestSessionJson
        {
            Assemblies = context.Assemblies.Select(x => x.ToJsonModel()).ToArray()
        };
    }

    public static TestAssemblyJson ToJsonModel(this AssemblyHookContext context)
    {
        return new TestAssemblyJson
        {
            AssemblyName = context.Assembly.GetName().FullName,
            Classes = context.TestClasses.Select(x => x.ToJsonModel()).ToArray()
        };
    }

    public static TestClassJson ToJsonModel(this ClassHookContext context)
    {
        return new TestClassJson
        {
            Type = context.ClassType.FullName,
            Tests = context.Tests.Select(x => x.ToJsonModel()).ToArray()
        };
    }

    public static TestJson ToJsonModel(this TestContext context)
    {
        var testDetails = context.TestDetails;
        if (testDetails == null)
        {
            throw new InvalidOperationException("TestDetails is null");
        }

        return new TestJson
        {
            Categories = testDetails.Categories,
            ClassType = testDetails.MethodMetadata.Class.Type.FullName ?? testDetails.ClassType.FullName ?? "Unknown",
            Result = context.Result?.ToJsonModel(),
            Timeout = testDetails.Timeout,
            CustomProperties = testDetails.CustomProperties.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>) kvp.Value.AsReadOnly()),
            DisplayName = context.GetDisplayName(),
            ObjectBag = context.ObjectBag,
            RetryLimit = testDetails.RetryLimit,
            ReturnType = testDetails.ReturnType?.FullName ?? "void",
            TestId = testDetails.TestId,
            TestName = testDetails.TestName,
            TestClassArguments = testDetails.TestClassArguments,
            TestFilePath = testDetails.TestFilePath,
            TestLineNumber = testDetails.TestLineNumber,
            TestMethodArguments = testDetails.TestMethodArguments,
            TestClassParameterTypes = testDetails.TestClassParameterTypes?.Select(x => x.FullName ?? "Unknown").ToArray() ?? [],
            TestMethodParameterTypes = testDetails.MethodMetadata.Parameters.Select(p => p.Type.FullName ?? "Unknown").ToArray(),
        };
    }

    public static TestResultJson ToJsonModel(this TestResult result)
    {
        return new TestResultJson
        {
            Duration = result.Duration,
            End = result.End,
            Exception = result.Exception?.ToJsonModel(),
            Output = result.Output,
            Start = result.Start,
            Status = result.State,
            ComputerName = result.ComputerName
        };
    }

    public static ExceptionJson ToJsonModel(this Exception exception)
    {
        return new ExceptionJson
        {
            Message = exception.Message,
            Stacktrace = exception.StackTrace,
            Type = exception.GetType().FullName,
            InnerException = exception.InnerException?.ToJsonModel()
        };
    }
}
