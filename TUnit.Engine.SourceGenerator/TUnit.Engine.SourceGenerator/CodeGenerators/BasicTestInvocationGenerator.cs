

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

internal static class BasicTestInvocationGenerator
{
    public static string GenerateTestInvocationCode(
        IMethodSymbol methodSymbol, 
        ClassInvocationString classInvocationString,
        IEnumerable<string> methodArguments,
        int currentCount)
    {
        var testId = TestInformationGenerator.GetTestId(methodSymbol, currentCount);

        var classType = methodSymbol.ContainingType;
        
        var fullyQualifiedClassType = classType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        return $$"""
                 global::TUnit.Core.TestDictionary.AddTest("{{testId}}", () => 
                        {
                 {{classInvocationString.ClassInvocation}};
             
                            var methodInfo = global::TUnit.Core.Helpers.MethodHelpers.GetMethodInfo(classInstance.{{methodSymbol.Name}});
                 
                            var testInformation = new global::TUnit.Core.TestInformation()
                            {
                                Categories = [{{string.Join(", ", TestInformationGenerator.GetCategories(methodSymbol))}}],
                                ClassInstance = classInstance,
                                ClassType = typeof({{fullyQualifiedClassType}}),
                                Timeout = {{TestInformationGenerator.GetTimeOut(methodSymbol)}},
                                TestClassArguments = classArgs,
                                TestMethodArguments = [{{string.Join(", ", methodArguments)}}],
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
                                TestBody = () => global::TUnit.Engine.RunHelpers.RunAsync(() => classInstance.{{GenerateTestMethodInvocation(methodSymbol)}}),
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