using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

internal static class GenericTestInvocationGenerator
{
    public static string GenerateTestInvocationCode(WriteableTest writeableTest)
    {
        var methodSymbol = writeableTest.MethodSymbol;
        var testId = writeableTest.TestId;

        var classType = methodSymbol.ContainingType;
        
        var fullyQualifiedClassType = classType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        return $$"""
                 global::TUnit.Core.TestDictionary.AddTest("{{testId}}", () => 
                        {
                 {{string.Join("\r\n", writeableTest.GetClassArgumentsInvocations().Select(x => $"\t\t{x}"))}}
                            var classInstance = {{writeableTest.ClassName}}({{writeableTest.GetClassArgumentVariableNamesAsList()}});             
                            var methodInfo = global::TUnit.Core.Helpers.MethodHelpers.GetMethodInfo(classInstance.{{methodSymbol.Name}});
                 
                 {{string.Join("\r\n", writeableTest.GetMethodArgumentsInvocations().Select(x => $"\t\t{x}"))}}
                 
                            var testInformation = new global::TUnit.Core.TestInformation()
                            {
                                Categories = [{{string.Join(", ", TestInformationGenerator.GetCategories(methodSymbol))}}],
                                ClassInstance = classInstance,
                                ClassType = typeof({{fullyQualifiedClassType}}),
                                Timeout = {{TestInformationGenerator.GetTimeOut(methodSymbol)}},
                                TestClassArguments = classArgs,
                                TestMethodArguments = [{{writeableTest.GetMethodArgumentVariableNamesAsList()}}],
                                TestClassParameterTypes = typeof({{fullyQualifiedClassType}}).GetConstructors().First().GetParameters().Select(x => x.ParameterType).ToArray(),
                                TestMethodParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray(),
                                NotInParallelConstraintKeys = {{TestInformationGenerator.GetNotInParallelConstraintKeys(methodSymbol)}},
                                RepeatCount = {{TestInformationGenerator.GetRepeatCount(methodSymbol)}},
                                RetryCount = {{TestInformationGenerator.GetRetryCount(methodSymbol)}},
                                MethodInfo = methodInfo,
                                TestName = "{{methodSymbol.Name}}",
                                CustomProperties = new global::System.Collections.Generic.Dictionary<string, string>()
                            };
                            
                            var testContext = new global::TUnit.Core.TestContext(testInformation);
                            
                            return new global::TUnit.Core.UnInvokedTest
                            {
                                Id = "{{testId}}",
                                TestContext = testContext,
                                OneTimeSetUps = [{{OneTimeSetUpWriter.GenerateCode(classType)}}],
                                BeforeEachTestSetUps = [{{SetUpWriter.GenerateCode(classType)}}],
                                TestClass = classInstance,
                                TestBody = () => global::TUnit.Engine.RunHelpers.RunAsync(() => classInstance.{{writeableTest.MethodName}}({{writeableTest.GetMethodArgumentVariableNamesAsList()}}),
                                AfterEachTestCleanUps = [{{CleanUpWriter.GenerateCode(classType)}}],
                                OneTimeCleanUps = [{{OneTimeCleanUpWriter.GenerateCode(classType)}}],
                            };
                        });
                 """;
    }
    
    private static string GenerateTestMethodInvocation(IMethodSymbol method, params string[] methodArguments)
    {
        var methodName = method.Name;

        var args = string.Join(", ", methodArguments);

        if (method.GetAttributes().Any(x =>
                x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                    is "global::TUnit.Core.TimeoutAttribute"))
        {
            // TODO : We don't want Engine cancellation token? We want a new linked one that'll cancel after the specified timeout in the attribute
            if(string.IsNullOrEmpty(args))
            {
                return $"{methodName}(EngineCancellationToken.Token)";
            }

            return $"{methodName}({args}, EngineCancellationToken.Token)";
        }
        
        return $"{methodName}({args})";
    }
}