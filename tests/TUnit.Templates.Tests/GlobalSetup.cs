using DiffEngine;

namespace TUnit.Templates.Tests;

public class GlobalSetup
{
    [Before(TestDiscovery)]
    public static void BeforeTestDiscovery()
    {
        DiffRunner.Disabled = true;
    }
}
