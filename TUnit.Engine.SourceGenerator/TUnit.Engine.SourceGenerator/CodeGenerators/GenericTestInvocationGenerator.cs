using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

internal static class GenericTestInvocationGenerator
{
    public static void GenerateTestInvocationCode(SourceCodeWriter sourceBuilder, WriteableTest writeableTest)
    {
        var methodSymbol = writeableTest.MethodSymbol;
        var classSymbol = writeableTest.ClassSymbol;
        var testId = writeableTest.TestId;
        
        AttributeData[] methodAndClassAttributes =
        [
            ..methodSymbol.GetAttributes(),
            ..classSymbol.GetAttributes(),
            ..methodSymbol.ContainingType.GetAttributes(),
        ];
        
        var fullyQualifiedClassType = classSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        
                 sourceBuilder.WriteLine("{");
                 
                 var classArguments = writeableTest.GetClassArgumentsInvocations();
                 var wasArgument = false;
                 foreach (var classArgument in classArguments)
                 {
                     wasArgument = true;
                     sourceBuilder.WriteLine(classArgument);
                 }

                 if (wasArgument)
                 {
                     sourceBuilder.WriteLine();
                 }
                 
                 sourceBuilder.WriteLine($"var resettableClassFactory = new global::TUnit.Core.ResettableLazy<{fullyQualifiedClassType}>(() => new {writeableTest.ClassName}({writeableTest.GetClassArgumentVariableNamesAsList()}));");           
                 sourceBuilder.WriteLine($"var methodInfo = global::TUnit.Core.Helpers.MethodHelpers.GetMethodInfo(() => resettableClassFactory.Value.{methodSymbol.Name});");

                 wasArgument = false;
                 var methodArguments = writeableTest.GetMethodArgumentsInvocations();
                 foreach (var methodArgument in methodArguments)
                 {
                     wasArgument = true;
                     sourceBuilder.WriteLine(methodArgument);
                 }
                 
                 if (wasArgument)
                 {
                     sourceBuilder.WriteLine();
                 }

                 sourceBuilder.WriteLine($"var testInformation = new global::TUnit.Core.TestInformation<{fullyQualifiedClassType}>()");
                 sourceBuilder.WriteLine("{");
                 sourceBuilder.WriteLine($"TestId = \"{testId}\",");
                 sourceBuilder.WriteLine($"Categories = [{string.Join(", ", TestInformationGenerator.GetCategories(methodAndClassAttributes))}],");
                 sourceBuilder.WriteLine("LazyClassInstance = resettableClassFactory,");
                 sourceBuilder.WriteLine($"ClassType = typeof({fullyQualifiedClassType}),");
                 sourceBuilder.WriteLine($"Timeout = {TestInformationGenerator.GetTimeOut(methodAndClassAttributes)},");
                 sourceBuilder.WriteLine($"TestClassArguments = [{writeableTest.GetClassArgumentVariableNamesAsList()}],");
                 sourceBuilder.WriteLine($"TestMethodArguments = [{writeableTest.GetMethodArgumentVariableNamesAsList()}],");
                 sourceBuilder.WriteLine($"TestClassParameterTypes = typeof({fullyQualifiedClassType}).GetConstructors().First().GetParameters().Select(x => x.ParameterType).ToArray(),");
                 sourceBuilder.WriteLine("TestMethodParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray(),");
                 sourceBuilder.WriteLine($"NotInParallelConstraintKeys = {TestInformationGenerator.GetNotInParallelConstraintKeys(methodAndClassAttributes)},");
                 sourceBuilder.WriteLine($"RepeatCount = {TestInformationGenerator.GetRepeatCount(methodAndClassAttributes)},");
                 sourceBuilder.WriteLine($"RetryCount = {TestInformationGenerator.GetRetryCount(methodAndClassAttributes)},");
                 sourceBuilder.WriteLine("MethodInfo = methodInfo,");
                 sourceBuilder.WriteLine($"TestName = \"{methodSymbol.Name}\",");

                 CustomPropertiesWriter.WriteCustomProperties(methodAndClassAttributes, sourceBuilder);
                 
                 sourceBuilder.WriteLine($"MethodRepeatCount = {writeableTest.CurrentMethodRepeatCount},");
                 sourceBuilder.WriteLine($"ClassRepeatCount = {writeableTest.CurrentClassRepeatCount},");
                 
                 var returnType = methodSymbol.ReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
                 if (returnType == "global::System.Void")
                 {
                     returnType = "void";
                 }
                 sourceBuilder.WriteLine($"ReturnType = typeof({returnType}),");
                 
                 sourceBuilder.WriteLine($"Order = {TestInformationGenerator.GetOrder(methodAndClassAttributes)},");

                 var testLocation = TestInformationGenerator.GetTestLocation(methodAndClassAttributes);
                 sourceBuilder.WriteLine($"TestFilePath = @\"{testLocation.FilePath}\",");
                 sourceBuilder.WriteLine($"TestLineNumber = {testLocation.LineNumber},");
                 
                 sourceBuilder.WriteLine("};");
                 sourceBuilder.WriteLine();
                 sourceBuilder.WriteLine("var testContext = new global::TUnit.Core.TestContext(testInformation);");
                 sourceBuilder.WriteLine();
                 sourceBuilder.WriteLine($"var unInvokedTest = new global::TUnit.Core.UnInvokedTest<{fullyQualifiedClassType}>(resettableClassFactory)");
                 sourceBuilder.WriteLine("{");
                 sourceBuilder.WriteLine($"Id = \"{testId}\",");
                 sourceBuilder.WriteLine("TestContext = testContext,");
                 sourceBuilder.WriteLine($"ApplicableTestAttributes = [{CustomTestAttributeGenerator.WriteCustomAttributes(classSymbol, methodSymbol)}],");
                 sourceBuilder.WriteLine($"BeforeEachTestSetUps = [{SetUpWriter.GenerateCode(classSymbol)}],");
                 sourceBuilder.WriteLine($"TestBody = classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.{writeableTest.MethodName}({writeableTest.GetMethodArgumentVariableNamesAsList()})),");
                 sourceBuilder.WriteLine($"AfterEachTestCleanUps = [{CleanUpWriter.GenerateCode(classSymbol)}],");
                 sourceBuilder.WriteLine("};");
                 sourceBuilder.WriteLine();
                 sourceBuilder.WriteLine($"global::TUnit.Core.TestDictionary.AddTest(\"{testId}\", unInvokedTest);");
                 sourceBuilder.WriteLine("}");
    }
}