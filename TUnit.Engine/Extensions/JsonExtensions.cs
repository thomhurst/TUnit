using TUnit.Core;
using TUnit.Engine.Json;

namespace TUnit.Engine.Extensions;

internal static class JsonExtensions
{
    public static TestSessionJson ToJsonModel(this TestSessionContext context)
    {
        var assemblies = new TestAssemblyJson[context.Assemblies.Count];
        for (var i = 0; i < context.Assemblies.Count; i++)
        {
            assemblies[i] = context.Assemblies[i].ToJsonModel();
        }

        return new TestSessionJson
        {
            Assemblies = assemblies
        };
    }

    public static TestAssemblyJson ToJsonModel(this AssemblyHookContext context)
    {
        var classes = new TestClassJson[context.TestClasses.Count];
        for (var i = 0; i < context.TestClasses.Count; i++)
        {
            classes[i] = context.TestClasses[i].ToJsonModel();
        }

        return new TestAssemblyJson
        {
            AssemblyName = context.Assembly.GetName().FullName,
            Classes = classes
        };
    }

    public static TestClassJson ToJsonModel(this ClassHookContext context)
    {
        var tests = new TestJson[context.Tests.Count];
        for (var i = 0; i < context.Tests.Count; i++)
        {
            tests[i] = context.Tests[i].ToJsonModel();
        }

        return new TestClassJson
        {
            Type = context.ClassType.FullName,
            Tests = tests
        };
    }

    public static TestJson ToJsonModel(this TestContext context)
    {
        var testDetails = context.TestDetails;
        if (testDetails == null)
        {
            throw new InvalidOperationException("TestDetails is null");
        }

        Type[]? classParameterTypes = testDetails.TestClassParameterTypes;
        string[] classParamTypeNames;
        if (classParameterTypes != null)
        {
            classParamTypeNames = new string[classParameterTypes.Length];
            for (var i = 0; i < classParameterTypes.Length; i++)
            {
                classParamTypeNames[i] = classParameterTypes[i].FullName ?? "Unknown";
            }
        }
        else
        {
            classParamTypeNames = [];
        }

        var methodParameters = testDetails.MethodMetadata.Parameters;
        var methodParamTypeNames = new string[methodParameters.Length];
        for (var i = 0; i < methodParameters.Length; i++)
        {
            methodParamTypeNames[i] = methodParameters[i].Type.FullName ?? "Unknown";
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
            TestClassParameterTypes = classParamTypeNames,
            TestMethodParameterTypes = methodParamTypeNames,
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
