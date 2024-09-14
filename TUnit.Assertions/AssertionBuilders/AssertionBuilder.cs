using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.Messages;

namespace TUnit.Assertions.AssertionBuilders;

public abstract class AssertionBuilder<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    internal StringBuilder? ExpressionBuilder { get; }
    internal string? RawActualExpression { get; }
    public AssertionMessage? AssertionMessage { get; protected set; }

    protected AssertionBuilder(string actual) : base(ConnectorType.None, null)
    {
        if (string.IsNullOrEmpty(actual))
        {
            RawActualExpression = null;
            ExpressionBuilder = null;
        }
        else
        {
            RawActualExpression = actual;
            ExpressionBuilder = new StringBuilder($"Assert.That({actual})");
        }
    }

    protected internal abstract Task<AssertionData<TActual>> GetAssertionData();

    internal AssertionBuilder<TActual, TAnd, TOr> AppendExpression(string expression)
    {
        ExpressionBuilder?.Append($".{expression}");
        return this;
    }
    
    internal AssertionBuilder<TActual, TAnd, TOr> AppendConnector(ConnectorType connectorType)
    {
        if (connectorType == ConnectorType.None)
        {
            return this;
        }

        if (ExpressionBuilder?.ToString().EndsWith($".{connectorType}") == true)
        {
            return this;
        }
        
        return AppendExpression(connectorType.ToString());
    }
    
    internal AssertionBuilder<TActual, TAnd, TOr> AppendCallerMethod(string? expression, [CallerMemberName] string methodName = "")
    {
        return AppendCallerMethodWithMultipleExpressions([expression], methodName);
    }
    
    internal AssertionBuilder<TActual, TAnd, TOr> AppendCallerMethodWithMultipleExpressions(string?[] expressions, [CallerMemberName] string methodName = "")
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return this;
        }

        return AppendExpression($"{methodName}({string.Join(", ", expressions)})");
    }
    
    [Obsolete("This is a base `object` method that should not be called.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerHidden]
    public new void Equals(object? obj)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }

    [Obsolete("This is a base `object` method that should not be called.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerHidden]
    public new void ReferenceEquals(object a, object b)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }
}