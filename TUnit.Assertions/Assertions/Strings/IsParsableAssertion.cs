using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Strings.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Assertions.Strings;

public class IsParsableAssertion<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] TTarget>
{
    private readonly bool _shouldBeParsable;
    private readonly string?[] _expressions;
    private IFormatProvider? _formatProvider;
    private readonly IValueSource<string?> _valueSource;

    public IsParsableAssertion(IValueSource<string?> valueSource, bool shouldBeParsable, params string?[] expressions)
    {
        _shouldBeParsable = shouldBeParsable;
        _expressions = expressions;
        _valueSource = valueSource;
    }

    /// <summary>
    /// Specifies the format provider to use when parsing the string.
    /// </summary>
    /// <param name="formatProvider">The format provider to use.</param>
    /// <returns>An assertion builder that will execute the parsing assertion.</returns>
    public AssertionBuilder<string?> WithFormatProvider(IFormatProvider formatProvider)
    {
        _formatProvider = formatProvider;

        // Add the appropriate condition based on whether it should be parsable
        if (_shouldBeParsable)
        {
            return _valueSource.RegisterAssertion(new StringIsParsableCondition<TTarget>(_formatProvider), _expressions);
        }
        else
        {
            return _valueSource.RegisterAssertion(new StringIsNotParsableCondition<TTarget>(_formatProvider), _expressions);
        }
    }

    // Default invocation without format provider
    public async ValueTask InvokeAsync()
    {
        AssertionBuilder<string?> builder;
        if (_formatProvider == null)
        {
            // Add the appropriate condition based on whether it should be parsable
            if (_shouldBeParsable)
            {
                builder = _valueSource.RegisterAssertion(new StringIsParsableCondition<TTarget>(CultureInfo.InvariantCulture), _expressions);
            }
            else
            {
                builder = _valueSource.RegisterAssertion(new StringIsNotParsableCondition<TTarget>(CultureInfo.InvariantCulture), _expressions);
            }

            // Process the assertions
            var data = await builder.GetAssertionData();
            await builder.ProcessAssertionsAsync(data);
        }
    }
}