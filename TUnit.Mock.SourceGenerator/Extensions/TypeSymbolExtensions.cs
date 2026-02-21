using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace TUnit.Mock.SourceGenerator.Extensions;

internal static class TypeSymbolExtensions
{
    public static string GetFullyQualifiedName(this ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public static string GetFullyQualifiedNameWithoutGlobal(this ITypeSymbol type)
    {
        var fqn = type.GetFullyQualifiedName();
        return fqn.StartsWith("global::") ? fqn.Substring("global::".Length) : fqn;
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
            var unbound = namedType.ConstructedFrom.GetFullyQualifiedNameWithoutGlobal();

            if (unbound == "System.Threading.Tasks.Task<T>" || namedType.ConstructedFrom.Name == "Task" && namedType.TypeArguments.Length == 1)
            {
                var innerType = namedType.TypeArguments[0];
                var innerDefault = innerType.GetSmartDefault(innerType.IsNullableAnnotated());
                return $"global::System.Threading.Tasks.Task.FromResult<{innerType.GetFullyQualifiedName()}>({innerDefault})";
            }

            if (unbound == "System.Threading.Tasks.ValueTask<T>" || namedType.ConstructedFrom.Name == "ValueTask" && namedType.TypeArguments.Length == 1)
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
            var paramTypes = string.Join(",", method.Parameters.Select(p => p.Type.GetFullyQualifiedName() + (p.RefKind != RefKind.None ? "&" : "")));
            var typeParams = method.TypeParameters.Length > 0 ? $"`{method.TypeParameters.Length}" : "";
            return $"M:{method.Name}{typeParams}({paramTypes})";
        }
        if (member is IPropertySymbol prop)
        {
            var paramTypes = string.Join(",", prop.Parameters.Select(p => p.Type.GetFullyQualifiedName()));
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
                return (named.TypeArguments[0].GetFullyQualifiedName(), false);
            }
        }
        return (type.GetFullyQualifiedName(), false);
    }
}
