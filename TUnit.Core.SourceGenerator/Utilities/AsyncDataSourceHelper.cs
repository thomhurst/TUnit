using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Helper methods for async data source conversion
/// </summary>
internal static class AsyncDataSourceHelper
{
    /// <summary>
    /// Generates the ConvertToSync helper method for async data sources
    /// </summary>
    public static void GenerateConvertToSyncMethod(CodeWriter writer)
    {
        writer.AppendLine("private static global::System.Collections.Generic.IEnumerable<object?[]> ConvertToSync(global::System.Func<global::System.Threading.CancellationToken, global::System.Collections.Generic.IAsyncEnumerable<object?[]>> asyncFactory)");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine("var cts = new global::System.Threading.CancellationTokenSource();");
        writer.AppendLine("var enumerator = asyncFactory(cts.Token).GetAsyncEnumerator(cts.Token);");
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("while (true)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("if (!enumerator.MoveNextAsync().AsTask().Wait(global::System.TimeSpan.FromSeconds(30)))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("break;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (AggregateException ae) when (ae.InnerException is OperationCanceledException)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("break;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("yield return enumerator.Current;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("finally");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("enumerator.DisposeAsync().AsTask().Wait();");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Ignore disposal errors for async enumerator");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("cts.Dispose();");
        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("}");
    }

    /// <summary>
    /// Determines if a method represents an async data source
    /// </summary>
    public static bool IsAsyncDataSource(IMethodSymbol method)
    {
        var returnType = method.ReturnType;

        // Check for Task<IEnumerable<T>>
        if (returnType is INamedTypeSymbol { Name: "Task", IsGenericType: true } namedType)
        {
            var innerType = namedType.TypeArguments.FirstOrDefault();
            if (innerType is INamedTypeSymbol innerNamed && IsEnumerableType(innerNamed))
            {
                return true;
            }
        }

        // Check for ValueTask<IEnumerable<T>>
        if (returnType is INamedTypeSymbol { Name: "ValueTask", IsGenericType: true } valueTaskType)
        {
            var innerType = valueTaskType.TypeArguments.FirstOrDefault();
            if (innerType is INamedTypeSymbol innerNamed && IsEnumerableType(innerNamed))
            {
                return true;
            }
        }

        // Check for IAsyncEnumerable<T>
        if (returnType is INamedTypeSymbol { Name: "IAsyncEnumerable", IsGenericType: true })
        {
            return true;
        }

        return false;
    }

    private static bool IsEnumerableType(INamedTypeSymbol type)
    {
        return type is { Name: "IEnumerable", IsGenericType: true };
    }
}
