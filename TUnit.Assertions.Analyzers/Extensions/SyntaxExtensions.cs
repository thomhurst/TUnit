using Microsoft.CodeAnalysis;

namespace TUnit.Assertions.Analyzers.Extensions;

public static class SyntaxExtensions
{
    public static IEnumerable<AssertionBuilder<TActual, TAnd, TOr>> GetAllAncestorSyntaxesOfType<AssertionBuilder<TActual, TAnd, TOr>>(this SyntaxNode input) 
        where AssertionBuilder<TActual, TAnd, TOr> : SyntaxNode
    {
        var parent = input.Parent;
        
        while (parent != null)
        {
            if (parent is AssertionBuilder<TActual, TAnd, TOr> output)
            {
                yield return output;
            }
            
            parent = parent.Parent;
        }
    }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr>? GetAncestorSyntaxOfType<AssertionBuilder<TActual, TAnd, TOr>>(this SyntaxNode input) 
        where AssertionBuilder<TActual, TAnd, TOr> : SyntaxNode
    {
        var parent = input.Parent;
        
        while (parent != null && parent is not AssertionBuilder<TActual, TAnd, TOr>)
        {
            parent = parent.Parent;
        }

        return parent as AssertionBuilder<TActual, TAnd, TOr>;
    }
        
    public static IEnumerable<IOperation> GetAncestorOperations(this IOperation operation) 
    {
        var parent = operation.Parent;
        
        while (parent != null)
        {
            yield return parent;
            parent = parent.Parent;
        } ;
    }
}