using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for parse assertions
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class ParseAssertion<TActual> : FluentAssertionBase<TActual, ParseAssertion<TActual>>
{
    internal ParseAssertion(AssertionBuilder<TActual> assertionBuilder)
        : base(assertionBuilder)
    {
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075", Justification = "FormatProvider property is preserved")]
    public ParseAssertion<TActual> WithFormatProvider(IFormatProvider formatProvider, [CallerArgumentExpression(nameof(formatProvider))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertion();
        if (assertion != null)
        {
            // The assertion condition has a constructor parameter or property for format provider
            var property = assertion.GetType().GetProperty("FormatProvider");
            property?.SetValue(assertion, formatProvider);
        }

        AppendCallerMethod([doNotPopulateThis]);
        return this;
    }
}