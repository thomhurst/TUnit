using TUnit.TestProject.Attributes;
using TUnit.TestProject.Enums;

namespace TUnit.TestProject;

public class EnumTests
{
    [EnumGenerator]
    [Test]
    public void Test(PriorityLevel priorityLevel)
    {
    }
}