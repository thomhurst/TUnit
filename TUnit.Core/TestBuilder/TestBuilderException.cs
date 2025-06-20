using System.Runtime.Serialization;

namespace TUnit.Core.TestBuilder;

/// <summary>
/// Base exception for TestBuilder-related errors.
/// </summary>
[Serializable]
public class TestBuilderException : Exception
{
    public TestBuilderException()
    {
    }

    public TestBuilderException(string message) : base(message)
    {
    }

    public TestBuilderException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected TestBuilderException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
    
    /// <summary>
    /// Gets or sets the test metadata that caused the exception.
    /// </summary>
    public TestMetadata? TestMetadata { get; set; }
    
    /// <summary>
    /// Gets or sets additional context about the error.
    /// </summary>
    public Dictionary<string, object?> Context { get; set; } = new();
}

/// <summary>
/// Exception thrown when test metadata is invalid or malformed.
/// </summary>
[Serializable]
public class InvalidTestMetadataException : TestBuilderException
{
    public InvalidTestMetadataException(string message, TestMetadata metadata) : base(message)
    {
        TestMetadata = metadata;
    }
    
    public InvalidTestMetadataException(string message, TestMetadata metadata, Exception innerException) 
        : base(message, innerException)
    {
        TestMetadata = metadata;
    }

    protected InvalidTestMetadataException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

/// <summary>
/// Exception thrown when a data source fails to provide data.
/// </summary>
[Serializable]
public class DataSourceException : TestBuilderException
{
    public DataSourceException(string dataSourceName, Exception innerException) 
        : base($"Data source '{dataSourceName}' failed to provide data", innerException)
    {
        DataSourceName = dataSourceName;
    }
    
    public DataSourceException(string dataSourceName, string message) 
        : base($"Data source '{dataSourceName}': {message}")
    {
        DataSourceName = dataSourceName;
    }

    protected DataSourceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        DataSourceName = info.GetString(nameof(DataSourceName)) ?? string.Empty;
    }
    
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(DataSourceName), DataSourceName);
    }
    
    /// <summary>
    /// Gets the name of the data source that failed.
    /// </summary>
    public string DataSourceName { get; }
}

/// <summary>
/// Exception thrown when test instantiation fails.
/// </summary>
[Serializable]
public class TestInstantiationException : TestBuilderException
{
    public TestInstantiationException(Type testClassType, Exception innerException) 
        : base($"Failed to instantiate test class '{testClassType.Name}'", innerException)
    {
        TestClassType = testClassType;
    }
    
    public TestInstantiationException(Type testClassType, string message) 
        : base($"Failed to instantiate test class '{testClassType.Name}': {message}")
    {
        TestClassType = testClassType;
    }

    protected TestInstantiationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        var typeName = info.GetString(nameof(TestClassType));
        TestClassType = Type.GetType(typeName ?? string.Empty) ?? typeof(object);
    }
    
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(TestClassType), TestClassType.AssemblyQualifiedName);
    }
    
    /// <summary>
    /// Gets the type of test class that failed to instantiate.
    /// </summary>
    public Type TestClassType { get; }
}

/// <summary>
/// Exception thrown when property injection fails.
/// </summary>
[Serializable]
public class PropertyInjectionException : TestBuilderException
{
    public PropertyInjectionException(string propertyName, Type propertyType, Exception innerException) 
        : base($"Failed to inject property '{propertyName}' of type '{propertyType.Name}'", innerException)
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
    }

    protected PropertyInjectionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        PropertyName = info.GetString(nameof(PropertyName)) ?? string.Empty;
        var typeName = info.GetString(nameof(PropertyType));
        PropertyType = Type.GetType(typeName ?? string.Empty) ?? typeof(object);
    }
    
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(PropertyName), PropertyName);
        info.AddValue(nameof(PropertyType), PropertyType.AssemblyQualifiedName);
    }
    
    /// <summary>
    /// Gets the name of the property that failed injection.
    /// </summary>
    public string PropertyName { get; }
    
    /// <summary>
    /// Gets the type of the property.
    /// </summary>
    public Type PropertyType { get; }
}