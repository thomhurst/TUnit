using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Mocks.Analyzers;

/// <summary>
/// Syntactic pre-filter for invocation analyzers. The analyzers in this project run for every
/// invocation in the compilation, and <c>SemanticModel.GetSymbolInfo</c> is by far their most
/// expensive step, so reject invocations whose method name can't match before binding them.
/// </summary>
internal static class InvocationNameFilter
{
    /// <summary>
    /// Returns the simple name the invocation calls (<c>Of</c> for <c>Mock.Of&lt;T&gt;()</c>,
    /// <c>x?.Of()</c>, <c>Of()</c>), or <c>null</c> when it can't be determined syntactically.
    /// A method's name can't be changed by aliases or <c>using static</c>, so the bound method's
    /// <c>Name</c> always equals this name.
    /// </summary>
    public static string? GetInvokedName(InvocationExpressionSyntax invocation)
    {
        return GetInvokedNameSyntax(invocation)?.Identifier.ValueText;
    }

    /// <summary>
    /// False only when the invocation certainly doesn't call a method named <paramref name="name"/>.
    /// </summary>
    public static bool MayInvoke(InvocationExpressionSyntax invocation, string name)
    {
        var invokedName = GetInvokedName(invocation);

        return invokedName is null || invokedName == name;
    }

    /// <summary>
    /// False only when the invocation certainly doesn't call a method named <paramref name="name1"/> or <paramref name="name2"/>.
    /// </summary>
    public static bool MayInvoke(InvocationExpressionSyntax invocation, string name1, string name2)
    {
        var invokedName = GetInvokedName(invocation);

        return invokedName is null || invokedName == name1 || invokedName == name2;
    }

    /// <summary>
    /// Like <see cref="MayInvoke(InvocationExpressionSyntax, string)"/>, but also false when the call site
    /// has no explicit type argument list. Only use it for generic methods whose type arguments can't be
    /// inferred (no parameter mentions them, e.g. <c>Mock.Of&lt;T&gt;()</c> or <c>Arg.IsNull&lt;T&gt;()</c>):
    /// every call to those is written <c>Name&lt;...&gt;(...)</c>, so a plain <c>Name(...)</c> call such
    /// as an assertion's <c>.IsNotNull()</c> can be skipped without binding it.
    /// </summary>
    public static bool MayInvokeGeneric(InvocationExpressionSyntax invocation, string name)
    {
        var invokedName = GetInvokedNameSyntax(invocation);

        return invokedName is null
            || invokedName is GenericNameSyntax && invokedName.Identifier.ValueText == name;
    }

    /// <summary>
    /// Two-name form of <see cref="MayInvokeGeneric(InvocationExpressionSyntax, string)"/>.
    /// </summary>
    public static bool MayInvokeGeneric(InvocationExpressionSyntax invocation, string name1, string name2)
    {
        var invokedName = GetInvokedNameSyntax(invocation);

        if (invokedName is null)
        {
            return true;
        }

        if (invokedName is not GenericNameSyntax)
        {
            return false;
        }

        var text = invokedName.Identifier.ValueText;
        return text == name1 || text == name2;
    }

    private static SimpleNameSyntax? GetInvokedNameSyntax(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
            SimpleNameSyntax simpleName => simpleName,
            _ => null,
        };
    }
}
