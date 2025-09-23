using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class BecauseExtensions
{
    public static TAssertionBuilder Because<TAssertionBuilder>(
        this TAssertionBuilder assertionBuilder, 
        string reason,
        [CallerArgumentExpression("reason")] string? reasonExpression = null) 
        where TAssertionBuilder : AssertionBuilder
    {
        // Store the reason in the assertion builder for use in error messages
        assertionBuilder.SetBecause(reason, reasonExpression);
        return assertionBuilder;
    }
}