namespace TUnit.Core;

/// <summary>
/// Represents an exception that occurs during test data generation.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown when there are problems generating test data from data sources,
/// such as invalid data, incompatible types, or errors in data source generators.
/// </para>
/// <para>
/// The exception captures context about the data source that failed and the test method
/// that was being prepared, providing detailed information to help diagnose data-related issues.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Example of a data source generator that might throw DataGenerationException
/// public class UserDataSource : DataSourceGeneratorAttribute&lt;User&gt;
/// {
///     public override IEnumerable&lt;Func&lt;User&gt;&gt; GenerateDataSources(DataGeneratorMetadata metadata)
///     {
///         var jsonFile = "users.json";
///
///         if (!File.Exists(jsonFile))
///         {
///             throw new DataGenerationException(
///                 $"User data file '{jsonFile}' not found",
///                 this);
///         }
///
///         try
///         {
///             var users = JsonSerializer.Deserialize&lt;List&lt;User&gt;&gt;(File.ReadAllText(jsonFile));
///
///             if (users == null || users.Count == 0)
///             {
///                 throw new DataGenerationException(
///                     "No valid user data found in JSON file",
///                     this);
///             }
///
///             return users.Select(user => () => user);
///         }
///         catch (JsonException ex)
///         {
///             throw new DataGenerationException(
///                 "Failed to deserialize user data from JSON",
///                 this,
///                 ex);
///         }
///     }
/// }
/// </code>
/// </example>
public class DataGenerationException : Exception
{
    /// <summary>
    /// Gets the data source object that caused the exception, if available.
    /// </summary>
    /// <value>The data source instance, or null if not applicable.</value>
    public object? DataSource { get; }

    /// <summary>
    /// Gets the type name of the data source that caused the exception.
    /// </summary>
    /// <value>The simple name of the data source type, or null if not available.</value>
    public string? DataSourceTypeName { get; }

    /// <summary>
    /// Gets the name of the test method that was being prepared when the exception occurred.
    /// </summary>
    /// <value>The test method name, or null if not set.</value>
    public string? TestMethodName { get; private set; }

    /// <summary>
    /// Gets the name of the test class that contains the test method.
    /// </summary>
    /// <value>The test class name, or null if not set.</value>
    public string? TestClassName { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGenerationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DataGenerationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGenerationException"/> class with a specified error message
    /// and a reference to the data source that caused the exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="dataSource">The data source object that caused the exception.</param>
    /// <remarks>
    /// The error message is automatically formatted to include the data source type name.
    /// </remarks>
    public DataGenerationException(string message, object? dataSource)
        : base(FormatMessage(message, dataSource))
    {
        DataSource = dataSource;
        DataSourceTypeName = dataSource?.GetType().Name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGenerationException"/> class with a specified error message,
    /// a reference to the data source that caused the exception, and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="dataSource">The data source object that caused the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DataGenerationException(string message, object? dataSource, Exception innerException)
        : base(FormatMessage(message, dataSource), innerException)
    {
        DataSource = dataSource;
        DataSourceTypeName = dataSource?.GetType().Name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataGenerationException"/> class with a specified error message
    /// and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DataGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Associates this exception with a specific test context.
    /// </summary>
    /// <param name="className">The name of the test class.</param>
    /// <param name="methodName">The name of the test method.</param>
    /// <returns>The current exception instance for method chaining.</returns>
    /// <remarks>
    /// This method is used internally to provide additional context about where the data generation failure occurred.
    /// The test context information is included in the exception's string representation.
    /// </remarks>
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