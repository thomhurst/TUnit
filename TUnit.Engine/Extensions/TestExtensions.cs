using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Extensions;

internal static class TestExtensions
{
    public static TestNode ToTestNode(this TestContext testContext)
    {
        var testDetails = testContext.TestDetails;
        
        var testNode = new TestNode
        {
            Uid = new TestNodeUid(testDetails.TestId),
            DisplayName = GetTestDisplayName(testContext),
            Properties = new PropertyBag(
            [
                new TestFileLocationProperty(testDetails.TestFilePath, new LinePositionSpan
                {
                    Start = new LinePosition(testDetails.TestLineNumber, 0),
                    End = new LinePosition(testDetails.TestLineNumber, 0)
                }),
                new TestMethodIdentifierProperty(
                    Namespace: testDetails.ClassType.Namespace!,
                    AssemblyFullName: testDetails.ClassType.Assembly.FullName!,
                    TypeName: GetClassTypeName(testContext),
                    MethodName: testDetails.TestName,
                    ParameterTypeFullNames: testDetails.TestMethodParameterTypes.Select(x => x.FullName!).ToArray(),
                    ReturnTypeFullName: testDetails.ReturnType.FullName!
                    ),
                
                // Custom TUnit Properties
                ..testDetails.Categories.Select(x => new KeyValuePairStringProperty("Category", x)),
                ..testDetails.CustomProperties.Select(x => new KeyValuePairStringProperty(x.Key, x.Value)),
                
                // TRX Report Properties
                new TrxFullyQualifiedTypeNameProperty(testDetails.ClassType.FullName!),
                new TrxCategoriesProperty([..testDetails.Categories]),
            ])
        };
        
        return testNode;
    }

    internal static string GetClassTypeName(this TestContext testContext)
    {
        var testDetails = testContext.TestDetails;
        
        var classTypeName = testDetails.ClassType.FullName?
                                .Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                                .LastOrDefault()
            ?? testDetails.ClassType.Name;
        
        if (testDetails.TestClassArguments.Length == 0)
        {
            return classTypeName;
        }
        
        return
            $"{classTypeName}({string.Join(", ", testDetails.TestClassArguments.Select(x => ArgumentFormatter.GetConstantValue(testContext, x)))})";
    }
    
    internal static string GetTestDisplayName(this TestContext testContext)
    {
        var testDetails = testContext.TestDetails;

        if (!string.IsNullOrWhiteSpace(testDetails.DisplayName))
        {
            return testDetails.DisplayName;
        }
        
        if (testDetails.TestMethodArguments.Length == 0)
        {
            return testDetails.TestName;
        }
        
        return
            $"{testDetails.TestName}({string.Join(", ", testDetails.TestMethodArguments.Select(x => ArgumentFormatter.GetConstantValue(testContext, x)))})";
    }

    public static TestNode WithProperty(this TestNode testNode, IProperty property)
    {
        testNode.Properties.Add(property);
        return testNode;
    }
    
    public static void ReRegisterTestWithArguments<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TTestClass>(this TestContext testContext, Func<TTestClass> classFactory, object[] methodArguments)
    {
        // TODO:
        // TestRegistrar.RegisterTest(new TestMetadata<TTestClass>
        // {
        //     TestId = Guid.NewGuid().ToString(),
        //     AttributeTypes = [],
        //     ClassConstructor = null,
        //     CurrentRepeatAttempt = 0,
        //     DataAttributes = [],
        //     MethodInfo = testContext.TestDetails.MethodInfo,
        //     ResettableClassFactory = new ResettableLazy<TTestClass>(classFactory),
        //     TestClassArguments = [],
        //     TestMethodArguments = methodArguments,
        //     ObjectBag = [],
        //     ParallelLimit = testContext.TestDetails.ParallelLimit,
        //     RepeatLimit = 0,
        //     TestExecutor = testContext.InternalDiscoveredTest.TestExecutor,
        //     TestClassProperties = [],
        //     TestFilePath = testContext.TestDetails.TestFilePath,
        //     TestLineNumber = testContext.TestDetails.TestLineNumber,
        //     TestMethodFactory = (@class, token) => AsyncConvert.Convert(testContext.TestDetails.MethodInfo.Invoke(@class, [..methodArguments, token]))
        // });
    }
}