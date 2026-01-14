using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Test generator V2 using primitives for incremental caching.
/// Generates ITestSource implementations for test methods.
/// </summary>
[Generator]
public sealed class TestMetadataGeneratorV2 : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource("V2_Init.g.cs", "// TestMetadataGeneratorV2 started");
        });

        var tests = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.TestAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) =>
                {
                    if (ctx.TargetSymbol is not IMethodSymbol method)
                        return ((string FullTypeName, string MinimalTypeName, string Namespace, string AssemblyName,
                            string MethodName, string FilePath, int LineNumber, string ReturnType, bool IsAsync, bool ReturnsVoid)?)null;
                    var containingType = method.ContainingType;
                    if (containingType == null || containingType.IsAbstract)
                        return null;
                    // Skip parameterized tests for now - V2 only handles simple parameterless tests
                    if (method.Parameters.Length > 0)
                        return null;
                    // Skip classes without parameterless constructors for now
                    if (!containingType.Constructors.Any(c => c.Parameters.Length == 0 && !c.IsStatic))
                        return null;
                    // Skip generic types for now
                    if (containingType.IsGenericType)
                        return null;
                    // Skip classes with required members (including inherited) for now
                    if (HasRequiredMembers(containingType))
                        return null;
                    // Only handle void or Task return types for now
                    var returnTypeName = method.ReturnType.ToDisplayString();
                    var isTask = returnTypeName == "System.Threading.Tasks.Task" || returnTypeName == "System.Threading.Tasks.Task<>";
                    var isVoid = method.ReturnsVoid;
                    if (!isVoid && !isTask)
                        return null;
                    var lineSpan = ctx.TargetNode.GetLocation().GetLineSpan();
                    var lineNumber = lineSpan.StartLinePosition.Line;
                    var filePath = lineSpan.Path ?? "";
                    var returnType = isVoid ? "void" : method.ReturnType.GloballyQualified();
                    var isAsync = method.IsAsync || isTask;
                    return (
                        FullTypeName: containingType.GloballyQualified(),
                        MinimalTypeName: containingType.Name,
                        Namespace: containingType.ContainingNamespace?.ToDisplayString() ?? "",
                        AssemblyName: containingType.ContainingAssembly?.Name ?? "",
                        MethodName: method.Name,
                        FilePath: filePath,
                        LineNumber: lineNumber,
                        ReturnType: returnType,
                        IsAsync: isAsync,
                        ReturnsVoid: method.ReturnsVoid
                    );
                })
            .Where(static s => s is not null);

        context.RegisterSourceOutput(tests, GenerateTestSource);
    }

    private static void GenerateTestSource(SourceProductionContext ctx,
        (string FullTypeName, string MinimalTypeName, string Namespace, string AssemblyName,
            string MethodName, string FilePath, int LineNumber, string ReturnType, bool IsAsync, bool ReturnsVoid)? model)
    {
        if (model == null) return;
        var m = model.Value;

        var safeName = GetSafeFileName(m.FullTypeName, m.MethodName, m.LineNumber);
        var code = GenerateCode(m);

        ctx.AddSource($"V2_{safeName}.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static string GetSafeFileName(string fullTypeName, string methodName, int lineNumber)
    {
        var rawName = $"{fullTypeName}_{methodName}_{lineNumber}";
        var safe = SanitizeFileName(rawName);
        if (safe.Length > 200)
        {
            var hash = GetStableHash(rawName);
            var shortType = fullTypeName.Split('.').LastOrDefault() ?? "Type";
            shortType = SanitizeFileName(shortType);
            safe = $"{shortType}_{methodName}_{hash:X8}";
        }
        return safe;
    }

    private static string SanitizeFileName(string name)
    {
        return name
            .Replace("global::", "")
            .Replace("::", "_")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "")
            .Replace("`", "_")
            .Replace("@", "_");
    }

    private static int GetStableHash(string str)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in str)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }

    private static string GenerateCode(
        (string FullTypeName, string MinimalTypeName, string Namespace, string AssemblyName,
            string MethodName, string FilePath, int LineNumber, string ReturnType, bool IsAsync, bool ReturnsVoid) m)
    {
        var cls = SanitizeFileName($"{m.FullTypeName}_{m.MethodName}_{m.LineNumber}");
        var escapedFilePath = m.FilePath.Replace("\\", "\\\\");
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.Generated;");
        sb.AppendLine();
        sb.AppendLine($"internal sealed class {cls}_TestSource : global::TUnit.Core.Interfaces.SourceGenerator.ITestSource");
        sb.AppendLine("{");
        sb.AppendLine($"    public async global::System.Collections.Generic.IAsyncEnumerable<global::TUnit.Core.TestMetadata> GetTestsAsync(string testSessionId, [global::System.Runtime.CompilerServices.EnumeratorCancellation] global::System.Threading.CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var metadata = new global::TUnit.Core.TestMetadata<{m.FullTypeName}>");
        sb.AppendLine("        {");
        sb.AppendLine($"            TestName = \"{m.MethodName}\",");
        sb.AppendLine($"            TestClassType = typeof({m.FullTypeName}),");
        sb.AppendLine($"            TestMethodName = \"{m.MethodName}\",");
        sb.AppendLine("            Dependencies = global::System.Array.Empty<global::TUnit.Core.TestDependency>(),");
        sb.AppendLine("            AttributeFactory = static () => [new global::TUnit.Core.TestAttribute()],");
        sb.AppendLine("            DataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        sb.AppendLine("            ClassDataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        sb.AppendLine("            PropertyDataSources = global::System.Array.Empty<global::TUnit.Core.PropertyDataSource>(),");
        sb.AppendLine("            PropertyInjections = global::System.Array.Empty<global::TUnit.Core.PropertyInjectionData>(),");
        sb.AppendLine("            InheritanceDepth = 0,");
        sb.AppendLine($"            FilePath = @\"{escapedFilePath}\",");
        sb.AppendLine($"            LineNumber = {m.LineNumber},");
        sb.AppendLine("            MethodMetadata = new global::TUnit.Core.MethodMetadata");
        sb.AppendLine("            {");
        sb.AppendLine($"                Type = typeof({m.FullTypeName}),");
        sb.AppendLine($"                TypeInfo = new global::TUnit.Core.ConcreteType(typeof({m.FullTypeName})),");
        sb.AppendLine($"                Name = \"{m.MethodName}\",");
        sb.AppendLine("                GenericTypeCount = 0,");
        if (m.ReturnsVoid)
        {
            sb.AppendLine("                ReturnType = typeof(void),");
            sb.AppendLine("                ReturnTypeInfo = new global::TUnit.Core.ConcreteType(typeof(void)),");
        }
        else
        {
            sb.AppendLine($"                ReturnType = typeof({m.ReturnType}),");
            sb.AppendLine($"                ReturnTypeInfo = new global::TUnit.Core.ConcreteType(typeof({m.ReturnType})),");
        }
        sb.AppendLine("                Parameters = global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
        sb.AppendLine($"                Class = global::TUnit.Core.ClassMetadata.GetOrAdd(\"{m.Namespace}:{m.FullTypeName}\", static () => new global::TUnit.Core.ClassMetadata");
        sb.AppendLine("                {");
        sb.AppendLine($"                    Type = typeof({m.FullTypeName}),");
        sb.AppendLine($"                    TypeInfo = new global::TUnit.Core.ConcreteType(typeof({m.FullTypeName})),");
        sb.AppendLine($"                    Name = \"{m.MinimalTypeName}\",");
        sb.AppendLine($"                    Namespace = \"{m.Namespace}\",");
        sb.AppendLine($"                    Assembly = global::TUnit.Core.AssemblyMetadata.GetOrAdd(\"{m.AssemblyName}\", static () => new global::TUnit.Core.AssemblyMetadata {{ Name = \"{m.AssemblyName}\" }}),");
        sb.AppendLine("                    Parameters = global::System.Array.Empty<global::TUnit.Core.ParameterMetadata>(),");
        sb.AppendLine("                    Properties = global::System.Array.Empty<global::TUnit.Core.PropertyMetadata>(),");
        sb.AppendLine("                    Parent = null");
        sb.AppendLine("                })");
        sb.AppendLine("            },");
        sb.AppendLine($"            InstanceFactory = (typeArgs, args) => new {m.FullTypeName}(),");
        sb.AppendLine("            InvokeTypedTest = static (instance, args, cancellationToken) =>");
        sb.AppendLine("            {");
        sb.AppendLine("                try");
        sb.AppendLine("                {");
        if (m.IsAsync || !m.ReturnsVoid)
        {
            sb.AppendLine($"                    return new global::System.Threading.Tasks.ValueTask(instance.{m.MethodName}());");
        }
        else
        {
            sb.AppendLine($"                    instance.{m.MethodName}();");
            sb.AppendLine("                    return default;");
        }
        sb.AppendLine("                }");
        sb.AppendLine("                catch (global::System.Exception ex)");
        sb.AppendLine("                {");
        sb.AppendLine("                    return new global::System.Threading.Tasks.ValueTask(global::System.Threading.Tasks.Task.FromException(ex));");
        sb.AppendLine("                }");
        sb.AppendLine("            },");
        sb.AppendLine("        };");
        sb.AppendLine("        metadata.UseRuntimeDataGeneration(testSessionId);");
        sb.AppendLine("        yield return metadata;");
        sb.AppendLine("        yield break;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"internal static class {cls}_ModuleInitializer");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    public static void Initialize()");
        sb.AppendLine("    {");
        sb.AppendLine($"        global::TUnit.Core.SourceRegistrar.Register(typeof({m.FullTypeName}), new {cls}_TestSource());");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static bool HasRequiredMembers(INamedTypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            var members = current.GetMembers();
            if (members.OfType<IPropertySymbol>().Any(p => p.IsRequired) ||
                members.OfType<IFieldSymbol>().Any(f => f.IsRequired))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
}
