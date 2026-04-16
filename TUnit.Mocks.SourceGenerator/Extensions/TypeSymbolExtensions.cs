using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TUnit.Mocks.SourceGenerator.Extensions;

internal static class TypeSymbolExtensions
{
    private static readonly SymbolDisplayFormat FullyQualifiedWithNullability =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat MinimallyQualifiedWithNullability =
        SymbolDisplayFormat.MinimallyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>
    /// Returns the fully qualified name without nullable annotations.
    /// For code generation that must preserve nullable reference type annotations,
    /// use <see cref="GetFullyQualifiedNameWithNullability"/> instead.
    /// </summary>
    public static string GetFullyQualifiedName(this ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public static string GetFullyQualifiedNameWithNullability(this ITypeSymbol type)
        => type.ToDisplayString(FullyQualifiedWithNullability);

    public static string GetMinimallyQualifiedNameWithNullability(this ITypeSymbol type)
        => type.ToDisplayString(MinimallyQualifiedWithNullability);

    public static string GetFullyQualifiedNameWithoutGlobal(this ITypeSymbol type)
    {
        var fqn = type.GetFullyQualifiedName();
        return fqn.StartsWith("global::") ? fqn.Substring("global::".Length) : fqn;
    }

    public static string GetOpenGenericTypeOfExpression(this INamedTypeSymbol type)
    {
        var definitionDisplay = type.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (!type.OriginalDefinition.IsGenericType)
            return definitionDisplay;

        var builder = new StringBuilder(definitionDisplay.Length);
        for (int i = 0; i < definitionDisplay.Length; i++)
        {
            var c = definitionDisplay[i];
            if (c != '<')
            {
                builder.Append(c);
                continue;
            }

            var depth = 1;
            var commaCount = 0;
            i++;
            while (i < definitionDisplay.Length && depth > 0)
            {
                c = definitionDisplay[i];
                if (c == '<')
                {
                    depth++;
                }
                else if (c == '>')
                {
                    depth--;
                }
                else if (c == ',' && depth == 1)
                {
                    commaCount++;
                }

                if (depth > 0)
                {
                    i++;
                }
            }

            builder.Append('<');
            builder.Append(',', commaCount);
            builder.Append('>');
        }

        return builder.ToString();
    }

    public static string GetGeneratedMockNamespace(this INamedTypeSymbol type)
    {
        var namespaceName = type.ContainingNamespace?.ToDisplayString() ?? "";
        return string.IsNullOrEmpty(namespaceName) || namespaceName == "<global namespace>"
            ? "TUnit.Mocks.Generated"
            : $"TUnit.Mocks.Generated.{namespaceName}";
    }

    public static string GetGeneratedMockBaseName(this INamedTypeSymbol type)
    {
        var originalDefinition = type.OriginalDefinition;
        var name = StripGlobalPrefix(originalDefinition.GetFullyQualifiedName());
        var namespaceName = originalDefinition.ContainingNamespace?.ToDisplayString() ?? "";

        if (!string.IsNullOrEmpty(namespaceName) && namespaceName != "<global namespace>")
        {
            var prefix = namespaceName + ".";
            if (name.StartsWith(prefix))
            {
                name = name.Substring(prefix.Length);
            }

            name = name.Replace("global::" + prefix, "");
        }

        return SanitizeIdentifier(name);
    }

    public static bool IsNullableAnnotated(this ITypeSymbol type)
    {
        return type.NullableAnnotation == NullableAnnotation.Annotated;
    }

    public static string GetSmartDefault(this ITypeSymbol returnType, bool isNullableAnnotated)
    {
        // Nullable types get null
        if (isNullableAnnotated)
            return "default";

        var fqn = returnType.GetFullyQualifiedNameWithoutGlobal();

        // String
        if (fqn == "string" || fqn == "System.String")
            return "\"\"";

        // Task
        if (fqn == "System.Threading.Tasks.Task")
            return "global::System.Threading.Tasks.Task.CompletedTask";

        // ValueTask (not generic)
        if (fqn == "System.Threading.Tasks.ValueTask")
            return "default";

        // Task<T>
        if (returnType is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            if (namedType.ConstructedFrom.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks"
                && namedType.ConstructedFrom.Name == "Task" && namedType.TypeArguments.Length == 1)
            {
                var innerType = namedType.TypeArguments[0];
                var innerDefault = innerType.GetSmartDefault(innerType.IsNullableAnnotated());
                return $"global::System.Threading.Tasks.Task.FromResult<{innerType.GetFullyQualifiedName()}>({innerDefault})";
            }

            if (namedType.ConstructedFrom.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks"
                && namedType.ConstructedFrom.Name == "ValueTask" && namedType.TypeArguments.Length == 1)
            {
                var innerType = namedType.TypeArguments[0];
                var innerDefault = innerType.GetSmartDefault(innerType.IsNullableAnnotated());
                return $"new global::System.Threading.Tasks.ValueTask<{innerType.GetFullyQualifiedName()}>({innerDefault})";
            }

            // IEnumerable<T>, IList<T>, ICollection<T>, IReadOnlyList<T>, IReadOnlyCollection<T>
            if (IsCollectionInterface(namedType))
            {
                var elementType = namedType.TypeArguments[0].GetFullyQualifiedName();
                return $"global::System.Array.Empty<{elementType}>()";
            }
        }

        // Value types
        if (returnType.IsValueType)
            return "default";

        // Non-nullable reference types that we can't give a smart default for
        return "default!";
    }

    private static bool IsCollectionInterface(INamedTypeSymbol type)
    {
        if (!type.IsGenericType || type.TypeArguments.Length != 1) return false;

        var ns = type.ConstructedFrom.ContainingNamespace?.ToDisplayString();
        if (ns is not "System.Collections.Generic") return false;

        var name = type.ConstructedFrom.Name;
        return name is "IEnumerable" or "IList" or "ICollection" or "IReadOnlyList" or "IReadOnlyCollection";
    }

    public static IEnumerable<ISymbol> GetAllInterfaceMembers(this ITypeSymbol type)
    {
        var seen = new HashSet<string>();

        // Direct members
        foreach (var member in type.GetMembers())
        {
            if (member.IsStatic) continue;
            var key = GetMemberKey(member);
            if (seen.Add(key))
                yield return member;
        }

        // Members from all interfaces
        foreach (var iface in type.AllInterfaces)
        {
            foreach (var member in iface.GetMembers())
            {
                if (member.IsStatic) continue;
                var key = GetMemberKey(member);
                if (seen.Add(key))
                    yield return member;
            }
        }
    }

    private static string GetMemberKey(ISymbol member)
    {
        if (member is IMethodSymbol method)
        {
            var paramTypes = string.Join(',', method.Parameters.Select(p => p.Type.GetFullyQualifiedName() + (p.RefKind != RefKind.None ? "&" : "")));
            var typeParams = method.TypeParameters.Length > 0 ? $"`{method.TypeParameters.Length}" : "";
            return $"M:{method.Name}{typeParams}({paramTypes})";
        }
        if (member is IPropertySymbol prop)
        {
            var paramTypes = string.Join(',', prop.Parameters.Select(p => p.Type.GetFullyQualifiedName()));
            return prop.Parameters.Length > 0 ? $"P:{prop.Name}[{paramTypes}]" : $"P:{prop.Name}";
        }
        if (member is IEventSymbol evt)
            return $"E:{evt.Name}";

        return $"O:{member.Name}";
    }

    /// <summary>
    /// Determines if a return type is a ValueTask or ValueTask{T}.
    /// </summary>
    public static bool IsValueTaskReturnType(this ITypeSymbol type)
    {
        var fqn = type.GetFullyQualifiedNameWithoutGlobal();
        if (fqn == "System.Threading.Tasks.ValueTask")
            return true;

        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            return named.ConstructedFrom.Name == "ValueTask";
        }
        return false;
    }

    /// <summary>
    /// Determines if a method returns a Task-like type (Task, Task{T}, ValueTask, ValueTask{T}).
    /// </summary>
    public static bool IsAsyncReturnType(this ITypeSymbol type)
    {
        var fqn = type.GetFullyQualifiedNameWithoutGlobal();
        if (fqn == "System.Threading.Tasks.Task" || fqn == "System.Threading.Tasks.ValueTask")
            return true;

        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var name = named.ConstructedFrom.Name;
            return name is "Task" or "ValueTask";
        }
        return false;
    }

    /// <summary>
    /// For Task{T} or ValueTask{T}, returns the inner T type symbol. For other types, returns null.
    /// </summary>
    public static ITypeSymbol? GetAsyncInnerTypeSymbol(this ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var name = named.ConstructedFrom.Name;
            if ((name == "Task" || name == "ValueTask") && named.TypeArguments.Length == 1)
            {
                return named.TypeArguments[0];
            }
        }
        return null;
    }

    /// <summary>
    /// For Task{T} or ValueTask{T}, returns the inner T. For Task/ValueTask, returns void equivalent.
    /// For non-async types, returns the type itself.
    /// </summary>
    public static (string UnwrappedType, bool IsVoidAsync) GetUnwrappedReturnType(this ITypeSymbol type)
    {
        var fqn = type.GetFullyQualifiedNameWithoutGlobal();
        if (fqn == "System.Threading.Tasks.Task" || fqn == "System.Threading.Tasks.ValueTask")
            return ("void", true);

        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var name = named.ConstructedFrom.Name;
            if ((name == "Task" || name == "ValueTask") && named.TypeArguments.Length == 1)
            {
                return (named.TypeArguments[0].GetFullyQualifiedNameWithNullability(), false);
            }
        }
        return (type.GetFullyQualifiedNameWithNullability(), false);
    }

    private static string StripGlobalPrefix(string name)
        => name.StartsWith("global::") ? name.Substring("global::".Length) : name;

    private static string SanitizeIdentifier(string name)
    {
        name = name.Replace("global::", "");

        var sb = new StringBuilder(name.Length);
        var lastWasUnderscore = false;

        foreach (var c in name)
        {
            if (c == ' ')
                continue;

            if (char.IsLetterOrDigit(c) || c == '_')
            {
                if (c == '_')
                {
                    if (lastWasUnderscore)
                        continue;

                    lastWasUnderscore = true;
                }
                else
                {
                    lastWasUnderscore = false;
                }

                sb.Append(c);
            }
            else if (!lastWasUnderscore)
            {
                sb.Append('_');
                lastWasUnderscore = true;
            }
        }

        return sb.ToString();
    }
}
