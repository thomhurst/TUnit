using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

internal class TestInformationProperty : IProperty
{
    public string UniqueId { get; }
    public string TestName { get; }
    public bool IsSingleTest { get; }
    public bool IsStatic { get; }

    public TestInformationProperty(string uniqueId, string testName, bool isSingleTest, bool isStatic)
    {
        UniqueId = uniqueId;
        TestName = testName;
        IsSingleTest = isSingleTest;
        IsStatic = isStatic;
    }
}