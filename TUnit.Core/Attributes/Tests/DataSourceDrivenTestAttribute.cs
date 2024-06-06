using System.Runtime.CompilerServices;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class DataSourceDrivenTestAttribute : BaseTestAttribute
{
    public DataSourceDrivenTestAttribute(
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0) : base(file, line)
    {
    }
}