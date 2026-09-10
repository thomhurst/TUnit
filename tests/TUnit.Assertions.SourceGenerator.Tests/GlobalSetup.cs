using DiffEngine;

namespace TUnit.Assertions.SourceGenerator.Tests;

public class GlobalSetup
{
    [Before(TestSession)]
    public static void SetUp()
    {
        DiffRunner.Disabled = true;
    }
}
