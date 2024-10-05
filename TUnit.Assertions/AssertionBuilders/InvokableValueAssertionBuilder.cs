using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableValueAssertionBuilder<TActual> : InvokableAssertionBuilder<TActual>, IValueSource<TActual>
{
    internal InvokableValueAssertionBuilder(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder.AssertionDataDelegate, invokableAssertionBuilder)
    {
    }

    /// <summary>
    /// Provide a formatted phrase explaining why the assertion is needed.<br />
    /// If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </summary>
    /// <param name="because">The composite format string explaining why the assertion is needed.</param>
    /// <param name="becauseArgs">The optional parameters used to replace the placeholders in <paramref name="because"/> with <see cref="string.Format(string,object[])" />.</param>
    public InvokableValueAssertionBuilder<TActual> Because([StringSyntax("CompositeFormat")] string because, params object[] becauseArgs)
    {
        var becauseReason = new BecauseReason(because, becauseArgs);
        foreach (var assertion in Assertions)
        {
            assertion.SetBecauseReason(becauseReason);
        }
        return this;
    }

    public AssertionBuilder<TActual> AssertionBuilder => this;
    
    public ValueAnd<TActual> And => new(AssertionBuilder.AppendConnector(ChainType.And));
    public ValueOr<TActual> Or => new(AssertionBuilder.AppendConnector(ChainType.Or));
}