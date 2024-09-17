﻿using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

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
    
    public static AssertionBuilder<TActual, TAnd, TOr>? GetAncestorSyntaxOfType<AssertionBuilder<TActual, TAnd, TOr>>(this SyntaxNode input) 
        where AssertionBuilder<TActual, TAnd, TOr> : SyntaxNode
    {
        var parent = input.Parent;
        
        while (parent != null && parent is not AssertionBuilder<TActual, TAnd, TOr>)
        {
            parent = parent.Parent;
        }

        return parent as AssertionBuilder<TActual, TAnd, TOr>;
    }
}