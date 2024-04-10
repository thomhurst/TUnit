using System.Linq;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

internal static class GenericTestInvocationGenerator
{
    public static string GenerateTestInvocationCode(WriteableTest writeableTest)
    {
        var methodSymbol = writeableTest.MethodSymbol;
        var classSymbol = writeableTest.ClassSymbol;
        var testId = writeableTest.TestId;
        
        var fullyQualifiedClassType = classSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        return $$"""
                 {
                 {{string.Join("\r\n", writeableTest.GetClassArgumentsInvocations().Select(x => $"\t\t{x}"))}}
                     var resettableClassFactory = new global::TUnit.Core.ResettableLazy<{{fullyQualifiedClassType}}>(() => new {{writeableTest.ClassName}}({{writeableTest.GetClassArgumentVariableNamesAsList()}}));             
                     var methodInfo = global::TUnit.Core.Helpers.MethodHelpers.GetMethodInfo(() => resettableClassFactory.Value.{{methodSymbol.Name}});
                 {{string.Join("\r\n", writeableTest.GetMethodArgumentsInvocations().Select(x => $"\t\t{x}"))}}
                     var testInformation = new global::TUnit.Core.TestInformation()
                     {
                         TestId = "{{testId}}",
                         Categories = [{{string.Join(", ", TestInformationGenerator.GetCategories(methodSymbol, classSymbol))}}],
                         ClassInstance = null, // TODO
                         ClassType = typeof({{fullyQualifiedClassType}}),
                         Timeout = {{TestInformationGenerator.GetTimeOut(methodSymbol, classSymbol)}},
                         TestClassArguments = [{{writeableTest.GetClassArgumentVariableNamesAsList()}}],
                         TestMethodArguments = [{{writeableTest.GetMethodArgumentVariableNamesAsList()}}],
                         TestClassParameterTypes = typeof({{fullyQualifiedClassType}}).GetConstructors().First().GetParameters().Select(x => x.ParameterType).ToArray(),
                         TestMethodParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray(),
                         NotInParallelConstraintKeys = {{TestInformationGenerator.GetNotInParallelConstraintKeys(methodSymbol, classSymbol)}},
                         RepeatCount = {{TestInformationGenerator.GetRepeatCount(methodSymbol, classSymbol)}},
                         RetryCount = {{TestInformationGenerator.GetRetryCount(methodSymbol, classSymbol)}},
                         MethodInfo = methodInfo,
                         TestName = "{{methodSymbol.Name}}",
                         CustomProperties = new global::System.Collections.Generic.Dictionary<string, string>(),
                         MethodRepeatCount = {{writeableTest.CurrentMethodRepeatCount}},
                         ClassRepeatCount = {{writeableTest.CurrentClassRepeatCount}},
                     };
                     
                     var testContext = new global::TUnit.Core.TestContext(testInformation);
                 
                    var unInvokedTest = new global::TUnit.Core.UnInvokedTest<{{fullyQualifiedClassType}}>(resettableClassFactory)
                         {
                             Id = "{{testId}}",
                             TestContext = testContext,
                             ApplicableTestAttributes = [{{CustomTestAttributeGenerator.WriteCustomAttributes(classSymbol, methodSymbol)}}],
                             OneTimeSetUps = [{{OneTimeSetUpWriter.GenerateCode(classSymbol)}}],
                             BeforeEachTestSetUps = [{{SetUpWriter.GenerateCode(classSymbol)}}],
                             TestBody = classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.{{writeableTest.MethodName}}({{writeableTest.GetMethodArgumentVariableNamesAsList()}})),
                             AfterEachTestCleanUps = [{{CleanUpWriter.GenerateCode(classSymbol)}}],
                         };
                 
                     global::TUnit.Core.TestDictionary.AddTest("{{testId}}", unInvokedTest);
                 }
                 """;
    }
}