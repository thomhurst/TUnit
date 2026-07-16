using DiffEngine;

namespace TUnit.Core.SourceGenerator.Tests;

public class GlobalSetup
{
    [Before(TestDiscovery)]
    public static void BeforeTestDiscovery()
    {
        DiffRunner.Disabled = true;
    }
}
