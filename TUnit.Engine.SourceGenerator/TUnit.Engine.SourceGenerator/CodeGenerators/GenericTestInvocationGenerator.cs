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
                 global::TUnit.Engine.OneTimeCleanUpOrchestrator.RegisterTest(typeof({{fullyQualifiedClassType}}));
                 {{OneTimeCleanUpWriter.GenerateLazyOneTimeCleanUpCode(classSymbol)}}
                 global::TUnit.Core.TestDictionary.AddTest("{{testId}}", () => 
                        {
                 {{string.Join("\r\n", writeableTest.GetClassArgumentsInvocations().Select(x => $"\t\t{x}"))}}
                            var classInstance = new {{writeableTest.ClassName}}({{writeableTest.GetClassArgumentVariableNamesAsList()}});             
                            var methodInfo = global::TUnit.Core.Helpers.MethodHelpers.GetMethodInfo(classInstance.{{methodSymbol.Name}});
                 {{string.Join("\r\n", writeableTest.GetMethodArgumentsInvocations().Select(x => $"\t\t{x}"))}}
                            var testInformation = new global::TUnit.Core.TestInformation()
                            {
                                Categories = [{{string.Join(", ", TestInformationGenerator.GetCategories(methodSymbol, classSymbol))}}],
                                ClassInstance = classInstance,
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
                                MethodRepeatCount = {{writeableTest.CurrentMethodCount}},
                                ClassRepeatCount = {{writeableTest.CurrentClassCount}},
                            };
                            
                            var testContext = new global::TUnit.Core.TestContext(testInformation);
                            
                            return new global::TUnit.Core.UnInvokedTest
                            {
                                Id = "{{testId}}",
                                TestContext = testContext,
                                OneTimeSetUps = [{{OneTimeSetUpWriter.GenerateCode(classSymbol)}}],
                                BeforeEachTestSetUps = [{{SetUpWriter.GenerateCode(classSymbol)}}],
                                TestClass = classInstance,
                                TestBody = () => global::TUnit.Engine.RunHelpers.RunAsync(() => classInstance.{{writeableTest.MethodName}}({{writeableTest.GetMethodArgumentVariableNamesAsList()}})),
                                AfterEachTestCleanUps = [{{CleanUpWriter.GenerateCode(classSymbol)}}],
                            };
                        });
                 """;
    }
}