using TUnit.Core.Enums;

namespace TUnit.Core;

/// <summary>
/// A compile-time friendly version of DataGeneratorMetadata that contains only
/// information available at source generation time.
/// </summary>
public record CompileTimeDataGeneratorMetadata : DataGeneratorMetadata
{
    public CompileTimeDataGeneratorMetadata()
    {
        // Set required properties to compile-time safe defaults
        TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext());
        TestSessionId = Guid.Empty.ToString();
        TestClassInstance = null;
        ClassInstanceArguments = null;
    }
}
