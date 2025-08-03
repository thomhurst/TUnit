namespace TUnit.Core;

public class DataGenerationException : Exception
{
    public object? DataSource { get; }
    public string? DataSourceTypeName { get; }
    public string? TestMethodName { get; private set; }
    public string? TestClassName { get; private set; }

    public DataGenerationException(string message) 
        : base(message)
    {
    }

    public DataGenerationException(string message, object? dataSource) 
        : base(FormatMessage(message, dataSource))
    {
        DataSource = dataSource;
        DataSourceTypeName = dataSource?.GetType().Name;
    }

    public DataGenerationException(string message, object? dataSource, Exception innerException) 
        : base(FormatMessage(message, dataSource), innerException)
    {
        DataSource = dataSource;
        DataSourceTypeName = dataSource?.GetType().Name;
    }

    public DataGenerationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public DataGenerationException WithTestContext(string? className, string? methodName)
    {
        TestClassName = className;
        TestMethodName = methodName;
        return this;
    }

    private static string FormatMessage(string message, object? dataSource)
    {
        if (dataSource == null)
        {
            return message;
        }

        var typeName = dataSource.GetType().Name;
        return $"{message} (Source: {typeName})";
    }

    public override string ToString()
    {
        var message = Message;
        
        if (!string.IsNullOrEmpty(TestClassName) || !string.IsNullOrEmpty(TestMethodName))
        {
            var context = string.Empty;
            if (!string.IsNullOrEmpty(TestClassName))
            {
                context = TestClassName;
            }
            if (!string.IsNullOrEmpty(TestMethodName))
            {
                context = string.IsNullOrEmpty(context) ? TestMethodName : $"{context}.{TestMethodName}";
            }
            
            message = $"[{context}] {message}";
        }

        if (InnerException != null)
        {
            return $"{message}\n---> {InnerException}";
        }

        return message;
    }
}