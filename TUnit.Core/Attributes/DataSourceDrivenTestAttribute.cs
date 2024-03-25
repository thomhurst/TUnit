using System.Runtime.CompilerServices;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class DataSourceDrivenTestAttribute : BaseTestAttribute
{
    public Type? ClassProvidingDataSource { get; }
    public string MethodNameProvidingDataSource { get; }
    

    public DataSourceDrivenTestAttribute(string methodNameProvidingDataSource,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) : base(file, line)
    {
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
    
    public DataSourceDrivenTestAttribute(Type classProvidingDataSource, 
        string methodNameProvidingDataSource, 
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) : base(file, line)
    {
        ClassProvidingDataSource = classProvidingDataSource;
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }
}