using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

/// <summary>
/// Generates module initializer for automatic test registration
/// </summary>
internal class ModuleInitializerGenerator
{
    private readonly StringBuilder _stringBuilder = new();
    
    public string GenerateModuleInitializer(List<TestMethodMetadata> testMethods, DiagnosticContext? diagnosticContext = null)
    {
        _stringBuilder.Clear();
        
        _stringBuilder.AppendLine("using System;");
        _stringBuilder.AppendLine("using System.Runtime.CompilerServices;");
        _stringBuilder.AppendLine("using System.Threading.Tasks;");
        _stringBuilder.AppendLine("using TUnit.Core;");
        _stringBuilder.AppendLine("using TUnit.Engine;");
        _stringBuilder.AppendLine();
        _stringBuilder.AppendLine("namespace TUnit.Generated;");
        _stringBuilder.AppendLine();
        _stringBuilder.AppendLine("/// <summary>");
        _stringBuilder.AppendLine("/// Module initializer for automatic test registration");
        _stringBuilder.AppendLine("/// </summary>");
        _stringBuilder.AppendLine("internal static class TUnitModuleInitializer");
        _stringBuilder.AppendLine("{");
        
        // Generate module initializer method
        _stringBuilder.AppendLine("    /// <summary>");
        _stringBuilder.AppendLine("    /// Automatically called during module loading to register all tests");
        _stringBuilder.AppendLine("    /// </summary>");
        _stringBuilder.AppendLine("    [ModuleInitializer]");
        _stringBuilder.AppendLine("    public static void Initialize()");
        _stringBuilder.AppendLine("    {");
        _stringBuilder.AppendLine("        try");
        _stringBuilder.AppendLine("        {");
        _stringBuilder.AppendLine("            RegisterAllDelegates();");
        _stringBuilder.AppendLine("            RegisterAllTests();");
        _stringBuilder.AppendLine("            RegisterWithDiscoveryService();");
        _stringBuilder.AppendLine("        }");
        _stringBuilder.AppendLine("        catch (Exception ex)");
        _stringBuilder.AppendLine("        {");
        _stringBuilder.AppendLine("            // Log initialization failure");
        _stringBuilder.AppendLine("            System.Console.Error.WriteLine($\"TUnit module initialization failed: {ex}\");");
        _stringBuilder.AppendLine("            throw;");
        _stringBuilder.AppendLine("        }");
        _stringBuilder.AppendLine("    }");
        _stringBuilder.AppendLine();
        
        // Generate delegate registration
        GenerateDelegateRegistration(testMethods);
        
        // Generate test registration
        GenerateTestRegistration(testMethods);
        
        // Generate discovery service registration
        GenerateDiscoveryServiceRegistration();
        
        _stringBuilder.AppendLine("}");
        return _stringBuilder.ToString();
    }
    
    private void GenerateDelegateRegistration(List<TestMethodMetadata> testMethods)
    {
        _stringBuilder.AppendLine("    /// <summary>");
        _stringBuilder.AppendLine("    /// Register all strongly-typed delegates");
        _stringBuilder.AppendLine("    /// </summary>");
        _stringBuilder.AppendLine("    private static void RegisterAllDelegates()");
        _stringBuilder.AppendLine("    {");
        
        // Track registered delegates to avoid duplicates
        var registeredDelegates = new HashSet<string>();
        
        foreach (var testMethod in testMethods)
        {
            var className = testMethod.TypeSymbol.ToDisplayString();
            var methodName = testMethod.MethodSymbol.Name;
            var delegateKey = $"{className}.{methodName}";
            
            if (registeredDelegates.Contains(delegateKey))
                continue;
                
            registeredDelegates.Add(delegateKey);
            
            var safeClassName = testMethod.TypeSymbol.Name.Replace(".", "_").Replace("<", "_").Replace(">", "_");
            var safeMethodName = methodName.Replace(".", "_");
            
            _stringBuilder.AppendLine($"        // Register delegate for {className}.{methodName}");
            _stringBuilder.AppendLine($"        var {safeClassName}_{safeMethodName}_Params = new[] {{");
            
            var parameters = testMethod.MethodSymbol.Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var comma = i < parameters.Length - 1 ? "," : "";
                _stringBuilder.AppendLine($"            typeof({param.Type.ToDisplayString()}){comma}");
            }
            
            _stringBuilder.AppendLine("        };");
            _stringBuilder.AppendLine($"        TestDelegateStorage.RegisterStronglyTypedDelegate(");
            _stringBuilder.AppendLine($"            \"{delegateKey}\",");
            _stringBuilder.AppendLine($"            {safeClassName}_{safeMethodName}_Params,");
            _stringBuilder.AppendLine($"            StronglyTypedTestDelegates.{safeClassName}_{safeMethodName});");
            _stringBuilder.AppendLine();
        }
        
        _stringBuilder.AppendLine("    }");
        _stringBuilder.AppendLine();
    }
    
    private void GenerateTestRegistration(List<TestMethodMetadata> testMethods)
    {
        _stringBuilder.AppendLine("    /// <summary>");
        _stringBuilder.AppendLine("    /// Register all test metadata");
        _stringBuilder.AppendLine("    /// </summary>");
        _stringBuilder.AppendLine("    private static void RegisterAllTests()");
        _stringBuilder.AppendLine("    {");
        _stringBuilder.AppendLine($"        var tests = new List<TestMetadata>({testMethods.Count});");
        _stringBuilder.AppendLine();
        
        foreach (var testMethod in testMethods)
        {
            var className = testMethod.TypeSymbol.ToDisplayString();
            var methodName = testMethod.MethodSymbol.Name;
            var testId = $"{className}.{methodName}";
            
            _stringBuilder.AppendLine("        tests.Add(new TestMetadata");
            _stringBuilder.AppendLine("        {");
            _stringBuilder.AppendLine($"            TestId = \"{testId}\",");
            _stringBuilder.AppendLine($"            TestName = \"{methodName}\",");
            _stringBuilder.AppendLine($"            TypeName = \"{className}\",");
            _stringBuilder.AppendLine($"            MethodName = \"{methodName}\",");
            _stringBuilder.AppendLine($"            FilePath = \"{testMethod.FilePath}\",");
            _stringBuilder.AppendLine($"            LineNumber = {testMethod.LineNumber},");
            _stringBuilder.AppendLine("            Categories = Array.Empty<string>(),");
            _stringBuilder.AppendLine("            SkipReason = null,");
            _stringBuilder.AppendLine("            IsSkipped = false,");
            _stringBuilder.AppendLine("            IsParallelizable = true,");
            _stringBuilder.AppendLine("            Timeout = TimeSpan.Zero,");
            _stringBuilder.AppendLine("            RepeatCount = 1,");
            _stringBuilder.AppendLine("            RetryCount = 0,");
            _stringBuilder.AppendLine("            Order = 0,");
            _stringBuilder.AppendLine("            Arguments = Array.Empty<object?>(),");
            _stringBuilder.AppendLine("            ParameterTypes = Array.Empty<Type>(),");
            _stringBuilder.AppendLine("            Hooks = new TestHooks(),");
            _stringBuilder.AppendLine("            DataSources = Array.Empty<DataSourceMetadata>(),");
            _stringBuilder.AppendLine("            PropertyInjection = Array.Empty<PropertyInjectionMetadata>(),");
            
            // Add invoker that uses strongly-typed delegate
            _stringBuilder.AppendLine($"            TestInvoker = async (instance, args) =>");
            _stringBuilder.AppendLine("            {");
            _stringBuilder.AppendLine($"                var typedDelegate = TestDelegateStorage.GetStronglyTypedDelegate(\"{testId}\");");
            _stringBuilder.AppendLine("                if (typedDelegate != null)");
            _stringBuilder.AppendLine("                {");
            _stringBuilder.AppendLine("                    // Use strongly-typed delegate (no boxing)");
            _stringBuilder.AppendLine($"                    var typedInstance = ({className})instance;");
            
            var parameters = testMethod.MethodSymbol.Parameters;
            if (parameters.Length == 0)
            {
                _stringBuilder.AppendLine("                    await ((Func<" + className + ", Task>)typedDelegate)(typedInstance);");
            }
            else
            {
                var delegateTypeParams = new List<string> { className };
                delegateTypeParams.AddRange(parameters.Select(p => p.Type.ToDisplayString()));
                delegateTypeParams.Add("Task");
                
                var delegateType = $"Func<{string.Join(", ", delegateTypeParams)}>";
                var argCasts = string.Join(", ", parameters.Select((p, i) => $"({p.Type.ToDisplayString()})args[{i}]"));
                
                _stringBuilder.AppendLine($"                    await (({delegateType})typedDelegate)(typedInstance, {argCasts});");
            }
            
            _stringBuilder.AppendLine("                }");
            _stringBuilder.AppendLine("                else");
            _stringBuilder.AppendLine("                {");
            _stringBuilder.AppendLine("                    throw new InvalidOperationException($\"No strongly-typed delegate found for test: " + testId + "\");");
            _stringBuilder.AppendLine("                }");
            _stringBuilder.AppendLine("            }");
            
            _stringBuilder.AppendLine("        });");
            _stringBuilder.AppendLine();
        }
        
        _stringBuilder.AppendLine("        // Store all registered tests");
        _stringBuilder.AppendLine("        foreach (var test in tests)");
        _stringBuilder.AppendLine("        {");
        _stringBuilder.AppendLine("            TestMetadataRegistry.RegisterTest(test);");
        _stringBuilder.AppendLine("        }");
        _stringBuilder.AppendLine("    }");
        _stringBuilder.AppendLine();
    }
    
    private void GenerateDiscoveryServiceRegistration()
    {
        _stringBuilder.AppendLine("    /// <summary>");
        _stringBuilder.AppendLine("    /// Register with the test discovery service");
        _stringBuilder.AppendLine("    /// </summary>");
        _stringBuilder.AppendLine("    private static void RegisterWithDiscoveryService()");
        _stringBuilder.AppendLine("    {");
        _stringBuilder.AppendLine("        var source = new SourceGeneratedTestMetadataSource(() => TestMetadataRegistry.GetAllTests());");
        _stringBuilder.AppendLine("        TestMetadataRegistry.RegisterSource(source);");
        _stringBuilder.AppendLine("    }");
    }
}