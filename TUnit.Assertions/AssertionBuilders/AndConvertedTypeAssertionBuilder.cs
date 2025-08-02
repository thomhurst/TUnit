using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

public class ConvertedDelegateAssertionBuilder<TException>(IDelegateSource source)
    : InvokableValueAssertionBuilder<TException>(new ConvertedDelegateSource<TException>(source)) where TException : Exception;


public class ConvertedValueAssertionBuilder<TFromType, TToType>(IValueSource<TFromType> source, ConvertToAssertCondition<TFromType, TToType> convertToAssertCondition)
    : InvokableValueAssertionBuilder<TToType>(new ConvertedValueSource<TFromType, TToType>(source, convertToAssertCondition));
