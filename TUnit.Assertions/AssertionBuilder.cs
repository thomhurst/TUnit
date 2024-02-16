using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public abstract class AssertionBuilder<TActual>
{
    public StringBuilder? ExpressionBuilder { get; }

    public AssertionBuilder(string? actual)
    {
        ExpressionBuilder = string.IsNullOrEmpty(actual) 
            ? null 
            : new StringBuilder($"Assert.That({actual})");
    }
    
    protected internal abstract Task<AssertionData<TActual>> GetAssertionData();

    internal AssertionBuilder<TActual> AppendExpression(string expression)
    {
        ExpressionBuilder?.Append($".{expression}");
        return this;
    }
    
    internal AssertionBuilder<TActual> AppendConnector(ConnectorType connectorType)
    {
        if (connectorType == ConnectorType.None)
        {
            return this;
        }
        
        return AppendExpression(connectorType.ToString());
    }
    
    internal AssertionBuilder<TActual> AppendCallerMethod(string? expectedExpression, [CallerMemberName] string methodName = "")
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return this;
        }

        return AppendExpression($"{methodName}({expectedExpression})");
    }
}