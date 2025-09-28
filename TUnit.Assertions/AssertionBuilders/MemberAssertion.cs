using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion for object member access
/// </summary>
public class MemberAssertion<TObject, TMember> : ValueAssertionBuilder<TMember>
{
    private readonly Func<Task<TObject>> _objectProvider;
    private readonly Expression<Func<TObject, TMember>> _memberSelector;
    private readonly Func<TObject, TMember> _compiledSelector;

    public MemberAssertion(Func<Task<TObject>> objectProvider, Expression<Func<TObject, TMember>> memberSelector)
        : base(async () =>
        {
            var obj = await objectProvider();
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }
            var compiled = memberSelector.Compile();
            return compiled(obj);
        })
    {
        _objectProvider = objectProvider;
        _memberSelector = memberSelector;
        _compiledSelector = memberSelector.Compile();
    }

    public new GenericEqualToAssertion<TMember> EqualTo(TMember expected)
    {
        return new GenericEqualToAssertion<TMember>(async () =>
        {
            var obj = await _objectProvider();
            if (obj == null)
            {
                throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
            }
            return _compiledSelector(obj);
        }, expected);
    }
}