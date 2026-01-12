using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers.Base;

/// <summary>
/// Transforms method signatures that contain await expressions but are not marked as async.
/// Converts void methods to async Task and T-returning methods to async Task&lt;T&gt;.
/// </summary>
public class AsyncMethodSignatureRewriter : CSharpSyntaxRewriter
{
    private readonly HashSet<string> _interfaceImplementingMethods;

    public AsyncMethodSignatureRewriter() : this(new HashSet<string>())
    {
    }

    public AsyncMethodSignatureRewriter(HashSet<string> interfaceImplementingMethods)
    {
        _interfaceImplementingMethods = interfaceImplementingMethods;
    }

    /// <summary>
    /// Collects method signatures that implement interface members.
    /// This should be called BEFORE syntax modifications while the semantic model is still valid.
    /// </summary>
    public static HashSet<string> CollectInterfaceImplementingMethods(
        CompilationUnitSyntax compilationUnit,
        SemanticModel semanticModel)
    {
        var methods = new HashSet<string>();

        foreach (var methodDecl in compilationUnit.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            // Check for explicit interface implementation syntax
            if (methodDecl.ExplicitInterfaceSpecifier != null)
            {
                methods.Add(GetMethodKey(methodDecl));
                continue;
            }

            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl);
            if (methodSymbol == null)
            {
                continue;
            }

            // Check if this method explicitly implements an interface
            if (methodSymbol.ExplicitInterfaceImplementations.Length > 0)
            {
                methods.Add(GetMethodKey(methodDecl));
                continue;
            }

            // Check if this method implicitly implements an interface member
            var containingType = methodSymbol.ContainingType;
            if (containingType != null)
            {
                foreach (var iface in containingType.AllInterfaces)
                {
                    foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
                    {
                        var impl = containingType.FindImplementationForInterfaceMember(member);
                        if (SymbolEqualityComparer.Default.Equals(impl, methodSymbol))
                        {
                            methods.Add(GetMethodKey(methodDecl));
                            break;
                        }
                    }
                }
            }
        }

        return methods;
    }

    /// <summary>
    /// Gets a unique key for a method declaration based on its signature.
    /// This key is stable across syntax tree modifications.
    /// </summary>
    private static string GetMethodKey(MethodDeclarationSyntax node)
    {
        // Build a key from class name, method name, and parameter types
        var className = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "";
        var methodName = node.Identifier.Text;
        var parameters = string.Join(",", node.ParameterList.Parameters.Select(p => p.Type?.ToString() ?? ""));
        return $"{className}.{methodName}({parameters})";
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // First, visit children to ensure nested content is processed
        node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;

        // Skip if already async or abstract
        if (node.Modifiers.Any(SyntaxKind.AsyncKeyword) ||
            node.Modifiers.Any(SyntaxKind.AbstractKeyword))
        {
            return node;
        }

        // Check if method contains await expressions
        bool hasAwait = node.DescendantNodes().OfType<AwaitExpressionSyntax>().Any();
        if (!hasAwait)
        {
            return node;
        }

        // Skip methods with ref/out/in parameters (they can't be async)
        if (node.ParameterList.Parameters.Any(p =>
            p.Modifiers.Any(SyntaxKind.RefKeyword) ||
            p.Modifiers.Any(SyntaxKind.OutKeyword) ||
            p.Modifiers.Any(SyntaxKind.InKeyword)))
        {
            return node;
        }

        // Skip if method implements an interface member (changing return type would break the implementation)
        if (ImplementsInterfaceMember(node))
        {
            return node;
        }

        // Convert the return type
        var newReturnType = ConvertReturnType(node.ReturnType);

        // Add async modifier after access modifiers but before other modifiers (like static)
        var newModifiers = InsertAsyncModifier(node.Modifiers);

        return node
            .WithReturnType(newReturnType)
            .WithModifiers(newModifiers);
    }

    private bool ImplementsInterfaceMember(MethodDeclarationSyntax node)
    {
        // Check for explicit interface implementation syntax (IFoo.Method)
        if (node.ExplicitInterfaceSpecifier != null)
        {
            return true;
        }

        // Check if this method was identified as an interface implementation
        var key = GetMethodKey(node);
        return _interfaceImplementingMethods.Contains(key);
    }

    private static TypeSyntax ConvertReturnType(TypeSyntax returnType)
    {
        // void -> Task
        if (returnType is PredefinedTypeSyntax predefined && predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
        {
            return SyntaxFactory.ParseTypeName("Task")
                .WithLeadingTrivia(returnType.GetLeadingTrivia())
                .WithTrailingTrivia(returnType.GetTrailingTrivia());
        }

        // T -> Task<T>
        var innerType = returnType.WithoutTrivia();
        return SyntaxFactory.GenericName("Task")
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(innerType)))
            .WithLeadingTrivia(returnType.GetLeadingTrivia())
            .WithTrailingTrivia(returnType.GetTrailingTrivia());
    }

    private static SyntaxTokenList InsertAsyncModifier(SyntaxTokenList modifiers)
    {
        // Find the right position for async (after public/private/etc, before static/virtual/etc)
        int insertIndex = 0;

        for (int i = 0; i < modifiers.Count; i++)
        {
            var modifier = modifiers[i];
            if (modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.PrivateKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword))
            {
                insertIndex = i + 1;
            }
        }

        var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        return modifiers.Insert(insertIndex, asyncModifier);
    }
}
