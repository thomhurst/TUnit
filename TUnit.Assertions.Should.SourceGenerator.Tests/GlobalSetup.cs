using DiffEngine;

namespace TUnit.Assertions.Should.SourceGenerator.Tests;

public class GlobalSetup
{
    [Before(TestSession)]
    public static void SetUp()
    {
        DiffRunner.Disabled = true;
    }
}
