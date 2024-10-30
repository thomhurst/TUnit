using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Engine.Json;

namespace TUnit.Engine.Extensions;

public static class JsonExtensions
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
            AssemblyName = context.Assembly.FullName,
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
        return new TestJson
        {
            Categories = context.TestDetails.Categories,
            Order = context.TestDetails.Order,
            ClassType = context.TestDetails.ClassType.FullName,
            Result = context.Result?.ToJsonModel(),
            Timeout = context.TestDetails.Timeout,
            CustomProperties = context.TestDetails.CustomProperties,
            DisplayName = context.GetTestDisplayName(),
            ObjectBag = context.ObjectBag,
            RetryLimit = context.TestDetails.RetryLimit,
            ReturnType = context.TestDetails.ReturnType.FullName,
            TestId = context.TestDetails.TestId,
            TestName = context.TestDetails.TestName,
            TestClassArguments = context.TestDetails.TestClassArguments,
            TestFilePath = context.TestDetails.TestFilePath,
            TestLineNumber = context.TestDetails.TestLineNumber,
            TestMethodArguments = context.TestDetails.TestMethodArguments,
            TestClassParameterTypes = context.TestDetails.TestClassParameterTypes.Select(x => x.FullName).ToArray(),
            TestMethodParameterTypes = context.TestDetails.TestMethodParameterTypes.Select(x => x.FullName).ToArray(),
            NotInParallelConstraintKeys = context.TestDetails.NotInParallelConstraintKeys
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
            Status = result.Status,
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