using System.Linq;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

internal static class GenericTestInvocationGenerator
{
    public static void GenerateTestInvocationCode(SourceCodeWriter sourceBuilder, WriteableTest writeableTest)
    {
        var methodSymbol = writeableTest.MethodSymbol;
        var classSymbol = writeableTest.ClassSymbol;
        var testId = writeableTest.TestId;
        
        var fullyQualifiedClassType = classSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        
                 sourceBuilder.WriteLine("{");
                 sourceBuilder.WriteLine(string.Join("\r\n", writeableTest.GetClassArgumentsInvocations().Select(x => $"\t\t{x}")));
                 sourceBuilder.WriteLine($"var resettableClassFactory = new global::TUnit.Core.ResettableLazy<{fullyQualifiedClassType}>(() => new {writeableTest.ClassName}({writeableTest.GetClassArgumentVariableNamesAsList()}));");           
                 sourceBuilder.WriteLine($"var methodInfo = global::TUnit.Core.Helpers.MethodHelpers.GetMethodInfo(() => resettableClassFactory.Value.{methodSymbol.Name});");
                 sourceBuilder.WriteLine(string.Join("\r\n", writeableTest.GetMethodArgumentsInvocations().Select(x => $"\t\t{x}")));
                 sourceBuilder.WriteLine("var testInformation = new global::TUnit.Core.TestInformation()");
                 sourceBuilder.WriteLine("{");
                 sourceBuilder.WriteLine($"TestId = \"{testId}\",");
                 sourceBuilder.WriteLine($"Categories = [{string.Join(", ", TestInformationGenerator.GetCategories(methodSymbol, classSymbol))}],");
                 sourceBuilder.WriteLine("ClassInstance = null, // TODO");
                 sourceBuilder.WriteLine($"ClassType = typeof({fullyQualifiedClassType}),");
                 sourceBuilder.WriteLine($"Timeout = {TestInformationGenerator.GetTimeOut(methodSymbol, classSymbol)},");
                 sourceBuilder.WriteLine($"TestClassArguments = [{writeableTest.GetClassArgumentVariableNamesAsList()}],");
                 sourceBuilder.WriteLine($"TestMethodArguments = [{writeableTest.GetMethodArgumentVariableNamesAsList()}],");
                 sourceBuilder.WriteLine($"TestClassParameterTypes = typeof({fullyQualifiedClassType}).GetConstructors().First().GetParameters().Select(x => x.ParameterType).ToArray(),");
                 sourceBuilder.WriteLine("TestMethodParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray(),");
                 sourceBuilder.WriteLine($"NotInParallelConstraintKeys = {TestInformationGenerator.GetNotInParallelConstraintKeys(methodSymbol, classSymbol)},");
                 sourceBuilder.WriteLine($"RepeatCount = {TestInformationGenerator.GetRepeatCount(methodSymbol, classSymbol)},");
                 sourceBuilder.WriteLine($"RetryCount = {TestInformationGenerator.GetRetryCount(methodSymbol, classSymbol)},");
                 sourceBuilder.WriteLine("MethodInfo = methodInfo,");
                 sourceBuilder.WriteLine($"TestName = \"{methodSymbol.Name}\",");
                 sourceBuilder.WriteLine("CustomProperties = new global::System.Collections.Generic.Dictionary<string, string>(),");
                 sourceBuilder.WriteLine($"MethodRepeatCount = {writeableTest.CurrentMethodRepeatCount},");
                 sourceBuilder.WriteLine($"ClassRepeatCount = {writeableTest.CurrentClassRepeatCount},");
                 sourceBuilder.WriteLine("};");
                 sourceBuilder.WriteLine();
                 sourceBuilder.WriteLine("var testContext = new global::TUnit.Core.TestContext(testInformation);");
                 sourceBuilder.WriteLine();
                 sourceBuilder.WriteLine($"var unInvokedTest = new global::TUnit.Core.UnInvokedTest<{fullyQualifiedClassType}>(resettableClassFactory)");
                 sourceBuilder.WriteLine("{");
                 sourceBuilder.WriteLine($"Id = \"{testId}\",");
                 sourceBuilder.WriteLine("TestContext = testContext,");
                 sourceBuilder.WriteLine($"ApplicableTestAttributes = [{CustomTestAttributeGenerator.WriteCustomAttributes(classSymbol, methodSymbol)}],");
                 sourceBuilder.WriteLine($"BeforeAllTestsInClasss = [{BeforeAllTestsInClassWriter.GenerateCode(classSymbol)}],");
                 sourceBuilder.WriteLine($"BeforeEachTestSetUps = [{SetUpWriter.GenerateCode(classSymbol)}],");
                 sourceBuilder.WriteLine($"TestBody = classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.{writeableTest.MethodName}({writeableTest.GetMethodArgumentVariableNamesAsList()})),");
                 sourceBuilder.WriteLine($"AfterEachTestCleanUps = [{CleanUpWriter.GenerateCode(classSymbol)}],");
                 sourceBuilder.WriteLine("};");
                 sourceBuilder.WriteLine();
                 sourceBuilder.WriteLine($"global::TUnit.Core.TestDictionary.AddTest(\"{testId}\", unInvokedTest);");
                 sourceBuilder.WriteLine("}");
    }
}