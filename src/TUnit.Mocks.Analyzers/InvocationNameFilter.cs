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
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
            SimpleNameSyntax simpleName => simpleName,
            _ => null,
        };

        return name?.Identifier.ValueText;
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
    /// False only when the invocation certainly doesn't call a method with one of the given names.
    /// </summary>
    public static bool MayInvoke(InvocationExpressionSyntax invocation, string name1, string name2, string name3)
    {
        var invokedName = GetInvokedName(invocation);

        return invokedName is null || invokedName == name1 || invokedName == name2 || invokedName == name3;
    }
}
