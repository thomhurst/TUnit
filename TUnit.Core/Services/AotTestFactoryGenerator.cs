using System.Reflection;
using System.Text;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// Generates AOT-safe test factories and invokers for source generation.
/// Creates the code that will be emitted by source generators.
/// </summary>
public class AotTestFactoryGenerator
{
    private readonly ICompileTimeDataResolver _dataResolver;
    private readonly CompileTimeSafetyAnalyzer _safetyAnalyzer;
    private readonly AotMethodDataSourceGenerator _methodDataSourceGenerator;
    private readonly AotAsyncDataSourceGenerator _asyncDataSourceGenerator;

    public AotTestFactoryGenerator(
        ICompileTimeDataResolver dataResolver,
        CompileTimeSafetyAnalyzer safetyAnalyzer,
        AotMethodDataSourceGenerator methodDataSourceGenerator,
        AotAsyncDataSourceGenerator asyncDataSourceGenerator)
    {
        _dataResolver = dataResolver;
        _safetyAnalyzer = safetyAnalyzer;
        _methodDataSourceGenerator = methodDataSourceGenerator;
        _asyncDataSourceGenerator = asyncDataSourceGenerator;
    }

    /// <summary>
    /// Generates AOT-safe strongly typed class factory code for a test class.
    /// </summary>
    /// <param name="classMetadata">The class metadata</param>
    /// <returns>Generated strongly typed factory code</returns>
    public string GenerateStronglyTypedClassFactory(ClassMetadata classMetadata)
    {
        var analysis = _safetyAnalyzer.AnalyzeClass(classMetadata);
        if (!analysis.IsSafe)
        {
            throw new InvalidOperationException(
                $"Class {classMetadata.Type.Name} is not AOT-safe: {string.Join(", ", analysis.Issues.Select(i => i.Message))}");
        }

        var className = classMetadata.Type.Name;
        var fullClassName = classMetadata.Type.FullName?.Replace('+', '.');
        var namespaceName = classMetadata.Type.Namespace ?? "Global";

        var code = new StringBuilder();
        code.AppendLine($"// Generated AOT-safe strongly typed factory for {fullClassName}");
        code.AppendLine($"public static class {className}StronglyTypedFactory");
        code.AppendLine("{");

        // Generate parameterless constructor factory (strongly typed)
        code.AppendLine($"    public static {fullClassName} CreateInstance()");
        code.AppendLine("    {");
        code.AppendLine($"        return new {fullClassName}();");
        code.AppendLine("    }");

        // Check if class has parameterized constructors
        var constructors = classMetadata.Type.GetConstructors();
        var parameterizedConstructors = constructors.Where(c => c.GetParameters().Length > 0).ToArray();

        if (parameterizedConstructors.Any())
        {
            // Generate strongly typed parameterized constructors
            foreach (var constructor in parameterizedConstructors)
            {
                var parameters = constructor.GetParameters();
                var paramList = string.Join(", ", parameters.Select(p => $"{GetTypeName(p.ParameterType)} {p.Name}"));
                var argList = string.Join(", ", parameters.Select(p => p.Name));

                code.AppendLine();
                code.AppendLine($"    public static {fullClassName} CreateInstance({paramList})");
                code.AppendLine("    {");
                code.AppendLine($"        return new {fullClassName}({argList});");
                code.AppendLine("    }");
            }

            // Generate factory delegate properties
            code.AppendLine();
            code.AppendLine($"    public static readonly Func<{fullClassName}> DefaultFactory = () => new {fullClassName}();");

            foreach (var constructor in parameterizedConstructors)
            {
                var parameters = constructor.GetParameters();
                var typeList = string.Join(", ", parameters.Select(p => GetTypeName(p.ParameterType)));
                var argList = string.Join(", ", parameters.Select(p => p.Name));
                var delegateSignature = parameters.Length > 0 ? $"Func<{typeList}, {fullClassName}>" : $"Func<{fullClassName}>";
                var factoryName = $"Factory{parameters.Length}Args";

                code.AppendLine($"    public static readonly {delegateSignature} {factoryName} = ({string.Join(", ", parameters.Select(p => p.Name))}) => new {fullClassName}({argList});");
            }
        }

        code.AppendLine("}");
        return code.ToString();
    }

    /// <summary>
    /// Generates AOT-safe strongly typed method invoker code for a test method.
    /// </summary>
    /// <param name="methodMetadata">The method metadata</param>
    /// <returns>Generated strongly typed invoker code</returns>
    public string GenerateStronglyTypedMethodInvoker(MethodMetadata methodMetadata)
    {
        var analysis = _safetyAnalyzer.AnalyzeMethod(methodMetadata);
        if (!analysis.IsSafe)
        {
            throw new InvalidOperationException(
                $"Method {methodMetadata.Name} is not AOT-safe: {string.Join(", ", analysis.Issues.Select(i => i.Message))}");
        }

        var className = methodMetadata.Class.Type.Name;
        var methodName = methodMetadata.Name;
        var fullClassName = methodMetadata.Class.Type.FullName?.Replace('+', '.');

        var code = new StringBuilder();
        code.AppendLine($"// Generated AOT-safe strongly typed invoker for {fullClassName}.{methodName}");
        code.AppendLine($"public static class {className}_{methodName}StronglyTypedInvoker");
        code.AppendLine("{");

        // Generate strongly typed invoke method
        var returnTypeName = GetTypeName(methodMetadata.ReturnType ?? typeof(void));
        var parameters = methodMetadata.Parameters;
        var paramList = string.Join(", ", parameters.Select(p => $"{GetTypeName(p.Type)} {p.Name}"));
        var fullParamList = parameters.Length > 0 ? $"{fullClassName} instance, {paramList}" : $"{fullClassName} instance";

        code.AppendLine($"    public static async Task<{returnTypeName}> InvokeAsync({fullParamList})");
        code.AppendLine("    {");

        var isVoid = methodMetadata.ReturnType == typeof(void);
        var isAsync = typeof(Task).IsAssignableFrom(methodMetadata.ReturnType ?? typeof(void));
        var argList = string.Join(", ", parameters.Select(p => p.Name));

        // Generate strongly typed method invocation
        if (isVoid)
        {
            if (isAsync)
            {
                code.AppendLine($"        await instance.{methodName}({argList});");
                code.AppendLine("        return default;");
            }
            else
            {
                code.AppendLine($"        instance.{methodName}({argList});");
                code.AppendLine("        return default;");
            }
        }
        else
        {
            if (isAsync)
            {
                code.AppendLine($"        return await instance.{methodName}({argList});");
            }
            else
            {
                code.AppendLine($"        return instance.{methodName}({argList});");
            }
        }

        code.AppendLine("    }");

        // Generate delegate property for this specific method signature
        var delegateType = GenerateMethodDelegateType(methodMetadata, fullClassName ?? "UnknownClass");
        code.AppendLine();
        code.AppendLine($"    public static readonly {delegateType} Invoker = InvokeAsync;");

        code.AppendLine("}");
        return code.ToString();
    }

    /// <summary>
    /// Generates AOT-safe property setter code for dependency injection.
    /// </summary>
    /// <param name="classMetadata">The class metadata</param>
    /// <param name="propertyName">The property name</param>
    /// <returns>Generated setter code</returns>
    public string GeneratePropertySetter(ClassMetadata classMetadata, string propertyName)
    {
        var property = classMetadata.Type.GetProperty(propertyName);
        if (property == null)
        {
            throw new ArgumentException($"Property {propertyName} not found on {classMetadata.Type.Name}");
        }

        if (!property.CanWrite)
        {
            throw new ArgumentException($"Property {propertyName} is not writable");
        }

        var className = classMetadata.Type.Name;
        var fullClassName = classMetadata.Type.FullName?.Replace('+', '.');
        var propertyType = property.PropertyType;

        var code = new StringBuilder();
        code.AppendLine($"// Generated AOT-safe property setter for {fullClassName}.{propertyName}");
        code.AppendLine($"public static class {className}_{propertyName}Setter");
        code.AppendLine("{");

        code.AppendLine("    public static void SetProperty(object instance, object? value)");
        code.AppendLine("    {");
        code.AppendLine($"        var typedInstance = ({fullClassName})instance;");

        if (propertyType.IsValueType)
        {
            code.AppendLine($"        typedInstance.{propertyName} = ({propertyType.Name})value!;");
        }
        else
        {
            code.AppendLine($"        typedInstance.{propertyName} = ({propertyType.FullName}?)value;");
        }

        code.AppendLine("    }");
        code.AppendLine("}");
        return code.ToString();
    }

    /// <summary>
    /// Generates strongly typed registration code for the global registry.
    /// </summary>
    /// <param name="testId">The test ID</param>
    /// <param name="classMetadata">The class metadata</param>
    /// <param name="methodMetadata">The method metadata</param>
    /// <returns>Generated strongly typed registration code</returns>
    public string GenerateStronglyTypedRegistrationCode(string testId, ClassMetadata classMetadata, MethodMetadata methodMetadata)
    {
        var className = classMetadata.Type.Name;
        var methodName = methodMetadata.Name;
        var fullClassName = classMetadata.Type.FullName?.Replace('+', '.');

        var code = new StringBuilder();
        code.AppendLine($"// Strongly typed registration for test {testId}");

        // Register strongly typed class factory
        code.AppendLine($"global::TUnit.Core.Services.TestExecutionRegistry.Instance.RegisterStronglyTypedClassFactory<{fullClassName}>(\"{testId}\", {className}StronglyTypedFactory.DefaultFactory);");

        // Register parameterized factories if available
        var constructors = classMetadata.Type.GetConstructors();
        var parameterizedConstructors = constructors.Where(c => c.GetParameters().Length > 0).ToArray();

        foreach (var constructor in parameterizedConstructors)
        {
            var parameters = constructor.GetParameters();
            var factoryName = $"Factory{parameters.Length}Args";
            code.AppendLine($"global::TUnit.Core.Services.TestExecutionRegistry.Instance.RegisterStronglyTypedClassFactory<{fullClassName}>(\"{testId}_{parameters.Length}args\", {className}StronglyTypedFactory.{factoryName});");
        }

        // Register strongly typed method invoker
        code.AppendLine($"global::TUnit.Core.Services.TestExecutionRegistry.Instance.RegisterStronglyTypedMethodInvoker<{fullClassName}>(\"{testId}\", {className}_{methodName}StronglyTypedInvoker.Invoker);");

        // Register method data source resolvers if the method has MethodDataSource attributes
        var methodDataSources = methodMetadata.GetMethodDataSourceAttributes();
        if (methodDataSources.Any())
        {
            var methodDataRegistration = _methodDataSourceGenerator.GenerateMethodDataSourceRegistration(testId, classMetadata, methodMetadata);
            code.AppendLine(methodDataRegistration);
        }

        // Register async data source resolvers if the method or class has AsyncDataSourceGenerator attributes
        var methodAsyncDataSources = methodMetadata.GetAsyncDataSourceGeneratorAttributes();
        var classAsyncDataSources = classMetadata.GetAsyncDataSourceGeneratorAttributes();
        if (methodAsyncDataSources.Any() || classAsyncDataSources.Any())
        {
            var asyncDataRegistration = _asyncDataSourceGenerator.GenerateAsyncDataSourceRegistration(testId, classMetadata, methodMetadata);
            code.AppendLine(asyncDataRegistration);
        }

        // Register property setters if needed
        var injectableProperties = classMetadata.Type.GetProperties()
            .Where(p => p.CanWrite && HasPropertyInjectionAttribute(p))
            .ToArray();

        foreach (var property in injectableProperties)
        {
            code.AppendLine($"global::TUnit.Core.Services.TestExecutionRegistry.Instance.RegisterPropertySetter(\"{testId}\", \"{property.Name}\", {className}_{property.Name}Setter.SetProperty);");
        }

        return code.ToString();
    }

    /// <summary>
    /// Generates registration code for the global registry (backward compatibility).
    /// </summary>
    /// <param name="testId">The test ID</param>
    /// <param name="classMetadata">The class metadata</param>
    /// <param name="methodMetadata">The method metadata</param>
    /// <returns>Generated registration code</returns>
    public string GenerateRegistrationCode(string testId, ClassMetadata classMetadata, MethodMetadata methodMetadata)
    {
        // Use strongly typed registration by default
        return GenerateStronglyTypedRegistrationCode(testId, classMetadata, methodMetadata);
    }

    private static bool HasPropertyInjectionAttribute(PropertyInfo property)
    {
        // Check for common property injection attributes
        return property.GetCustomAttributes(false).Any(attr =>
            attr.GetType().Name.Contains("Inject") ||
            attr.GetType().Name.Contains("Property"));
    }

    /// <summary>
    /// Generates the delegate type for a strongly typed method invoker.
    /// </summary>
    private string GenerateMethodDelegateType(MethodMetadata methodMetadata, string className)
    {
        var parameters = methodMetadata.Parameters;
        var returnType = methodMetadata.ReturnType ?? typeof(void);
        var isAsync = typeof(Task).IsAssignableFrom(returnType);

        var paramTypes = new List<string> { className };
        paramTypes.AddRange(parameters.Select(p => GetTypeName(p.Type)));

        var returnTypeName = isAsync ? GetTypeName(returnType) : $"Task<{GetTypeName(returnType)}>";

        return $"Func<{string.Join(", ", paramTypes)}, {returnTypeName}>";
    }

    /// <summary>
    /// Gets a clean type name for code generation.
    /// </summary>
    private static string GetTypeName(Type type)
    {
        if (type == typeof(void))
        {
            return "void";
        }

        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        if (type == typeof(double))
        {
            return "double";
        }

        if (type == typeof(float))
        {
            return "float";
        }

        if (type == typeof(long))
        {
            return "long";
        }

        if (type == typeof(short))
        {
            return "short";
        }

        if (type == typeof(byte))
        {
            return "byte";
        }

        if (type == typeof(char))
        {
            return "char";
        }

        if (type == typeof(decimal))
        {
            return "decimal";
        }

        if (type.IsGenericType)
        {
            var genericTypeName = type.Name.Split('`')[0];
            var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeName));
            return $"{genericTypeName}<{genericArgs}>";
        }

        return type.FullName?.Replace('+', '.') ?? type.Name;
    }

    /// <summary>
    /// Generates strongly typed class factory (backward compatibility wrapper).
    /// </summary>
    public string GenerateClassFactory(ClassMetadata classMetadata)
    {
        return GenerateStronglyTypedClassFactory(classMetadata);
    }

    /// <summary>
    /// Generates all MethodDataSource factories for a test class.
    /// </summary>
    /// <param name="classMetadata">The test class metadata</param>
    /// <param name="testMethods">The test methods in the class</param>
    /// <returns>Generated MethodDataSource factory code</returns>
    public string GenerateMethodDataSourceFactories(ClassMetadata classMetadata, IEnumerable<MethodMetadata> testMethods)
    {
        var code = new StringBuilder();
        var className = classMetadata.Type.Name;
        var fullClassName = classMetadata.Type.FullName?.Replace('+', '.');

        code.AppendLine($"// Generated AOT-safe MethodDataSource factories for {fullClassName}");
        code.AppendLine();

        // Collect all unique MethodDataSource attributes across all test methods
        var allMethodDataSources = new HashSet<string>();

        foreach (var method in testMethods)
        {
            var methodDataSources = method.GetMethodDataSourceAttributes();
            foreach (var methodDataSource in methodDataSources)
            {
                allMethodDataSources.Add(methodDataSource.MethodNameProvidingDataSource);
            }
        }

        // Generate factories for each unique data source method
        foreach (var methodName in allMethodDataSources)
        {
            try
            {
                var methodDataSourceAttr = new MethodDataSourceAttribute(methodName);
                var factoryCode = _methodDataSourceGenerator.GenerateMethodDataSourceFactory(methodDataSourceAttr, classMetadata.Type);
                code.AppendLine(factoryCode);
                code.AppendLine();
            }
            catch (Exception ex)
            {
                code.AppendLine($"// Error generating MethodDataSource factory for {methodName}: {ex.Message}");
                code.AppendLine();
            }
        }

        // Generate resolvers for each test method that has MethodDataSource attributes
        foreach (var method in testMethods)
        {
            var methodDataSources = method.GetMethodDataSourceAttributes();
            if (methodDataSources.Any())
            {
                try
                {
                    var resolverCode = _methodDataSourceGenerator.GenerateMethodDataSourceResolver(classMetadata, method);
                    code.AppendLine(resolverCode);
                    code.AppendLine();
                }
                catch (Exception ex)
                {
                    code.AppendLine($"// Error generating MethodDataSource resolver for {method.Name}: {ex.Message}");
                    code.AppendLine();
                }
            }
        }

        return code.ToString();
    }

    /// <summary>
    /// Generates all AsyncDataSource factories for a test class.
    /// </summary>
    /// <param name="classMetadata">The test class metadata</param>
    /// <param name="testMethods">The test methods in the class</param>
    /// <returns>Generated AsyncDataSource factory code</returns>
    public string GenerateAsyncDataSourceFactories(ClassMetadata classMetadata, IEnumerable<MethodMetadata> testMethods)
    {
        try
        {
            return _asyncDataSourceGenerator.GenerateAsyncDataSourceFactories(classMetadata, testMethods);
        }
        catch (Exception ex)
        {
            return $"// Error generating AsyncDataSource factories: {ex.Message}";
        }
    }

    /// <summary>
    /// Generates strongly typed method invoker (backward compatibility wrapper).
    /// </summary>
    public string GenerateMethodInvoker(MethodMetadata methodMetadata)
    {
        return GenerateStronglyTypedMethodInvoker(methodMetadata);
    }
}
