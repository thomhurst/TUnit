using System.Runtime.CompilerServices;
using System.Text;

namespace TUnit.Assertions.AssertionBuilders;

public class ExpressionFormatter
{
    private readonly StringBuilder _builder;
    private readonly string? _actualExpression;

    public ExpressionFormatter(string? actualExpression)
    {
        _actualExpression = actualExpression;
        
        if (string.IsNullOrEmpty(actualExpression))
        {
            _builder = new StringBuilder("Assert.That(UNKNOWN)");
        }
        else
        {
            _builder = new StringBuilder($"Assert.That({actualExpression})");
        }
    }

    public void AppendMethod(string methodName, params string?[] arguments)
    {
        _builder.Append('.');
        _builder.Append(methodName);
        _builder.Append('(');
        
        for (var i = 0; i < arguments.Length; i++)
        {
            _builder.Append(arguments[i]);
            if (i < arguments.Length - 1)
            {
                _builder.Append(", ");
            }
        }
        
        _builder.Append(')');
    }

    public void AppendConnector(string connector)
    {
        if (!string.IsNullOrEmpty(connector))
        {
            _builder.Append('.');
            _builder.Append(connector);
        }
    }

    public string GetExpression()
    {
        var expression = _builder.ToString();
        return expression.Length > 100 ? $"{expression[..100]}..." : expression;
    }

    public string? ActualExpression => _actualExpression;
}