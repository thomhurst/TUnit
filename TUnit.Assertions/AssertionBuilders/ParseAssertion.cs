using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Strings.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Clean parse assertion - no inheritance, just configuration
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class ParseAssertion<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
    TTarget>
{
    private readonly IValueSource<string?> _source;
    private readonly string?[] _expressions;

    // Configuration
    private IFormatProvider? _formatProvider;

    internal ParseAssertion(IValueSource<string?> source, string?[] expressions)
    {
        _source = source;
        _expressions = expressions;
    }

    public ParseAssertion<TTarget> WithFormatProvider(IFormatProvider formatProvider, [CallerArgumentExpression(nameof(formatProvider))] string doNotPopulateThis = "")
    {
        _formatProvider = formatProvider;
        return this;
    }

    public TaskAwaiter GetAwaiter()
    {
        return ExecuteAsync().GetAwaiter();
    }

    private async Task ExecuteAsync()
    {
        // Create condition with all configuration
        var condition = new ParseConversionAssertCondition<TTarget>(_formatProvider);

        // Register and execute
        var builder = _source.RegisterConversionAssertion(condition, _expressions);
        var data = await builder.GetAssertionData();
        await builder.ProcessAssertionsAsync(data);
    }
}
