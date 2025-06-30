using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Responsible for generating strongly-typed delegates for test execution
/// </summary>
internal sealed class DelegateGenerator
{
    /// <summary>
    /// Generates delegate registration code for all test methods
    /// </summary>
    public void GenerateDelegateRegistrations(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        writer.AppendLine("// Registering delegates for AOT execution");
        
        foreach (var testInfo in testMethods)
        {
            // Skip generic methods - they need special handling
            if (testInfo.MethodSymbol.IsGenericMethod)
                continue;
                
            GenerateInstanceFactoryRegistration(writer, testInfo);
            GenerateTestInvokerRegistration(writer, testInfo);
        }
    }

    /// <summary>
    /// Generates strongly-typed delegate definitions inline
    /// </summary>
    public void GenerateStronglyTypedDelegates(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        var processedMethods = new HashSet<string>();
        
        foreach (var testInfo in testMethods)
        {
            var delegateName = GetTestDelegateName(testInfo);
            
            if (processedMethods.Add(delegateName))
            {
                GenerateTestDelegate(writer, testInfo);
            }
        }
    }

    private void GenerateInstanceFactoryRegistration(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var safeClassName = testInfo.TypeSymbol.Name.Replace(".", "_");
        
        writer.AppendLine($"TestDelegateStorage.RegisterInstanceFactory(\"{className}\", args => new {className}({GenerateConstructorArgs(testInfo)}));");
    }

    private void GenerateTestInvokerRegistration(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        var invokerName = GetTestDelegateName(testInfo);
        
        writer.AppendLine($"TestDelegateStorage.RegisterTestInvoker(\"{className}.{methodName}\", {invokerName});");
    }

    private void GenerateTestDelegate(CodeWriter writer, TestMethodMetadata testInfo)
    {
        // Skip generic methods - they need special handling
        if (testInfo.MethodSymbol.IsGenericMethod)
        {
            return;
        }

        var delegateName = GetTestDelegateName(testInfo);
        var className = testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testInfo.MethodSymbol.Name;
        
        writer.AppendLine($"private static async Task {delegateName}(object instance, object?[] args)");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"var typedInstance = ({className})instance;");
        
        // Generate parameter unpacking
        var parameters = testInfo.MethodSymbol.Parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"var arg{i} = ({paramType})args[{i}]!;");
        }
        
        // Generate method invocation
        var argList = string.Join(", ", Enumerable.Range(0, parameters.Length).Select(i => $"arg{i}"));
        
        if (testInfo.MethodSymbol.IsAsync)
        {
            writer.AppendLine($"await typedInstance.{methodName}({argList});");
        }
        else
        {
            writer.AppendLine($"typedInstance.{methodName}({argList});");
        }
        
        writer.Unindent();
        writer.AppendLine("}");
    }

    private string GetTestDelegateName(TestMethodMetadata testInfo)
    {
        var className = testInfo.TypeSymbol.Name.Replace(".", "_");
        var methodName = testInfo.MethodSymbol.Name;
        return $"{className}_{methodName}_Invoker";
    }

    private string GenerateConstructorArgs(TestMethodMetadata testInfo)
    {
        var constructors = testInfo.TypeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic && m.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        if (!constructors.Any() || constructors.All(c => c.Parameters.Length == 0))
        {
            return string.Empty;
        }

        // Find the constructor with parameters and generate cast arguments
        var constructor = constructors.FirstOrDefault(c => c.Parameters.Length > 0);
        if (constructor == null)
        {
            return string.Empty;
        }

        var argList = new List<string>();
        for (int i = 0; i < constructor.Parameters.Length; i++)
        {
            var param = constructor.Parameters[i];
            var paramType = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            argList.Add($"({paramType})args[{i}]");
        }

        return string.Join(", ", argList);
    }
}