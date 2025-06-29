using DiffEngine;

namespace TUnit.PublicAPI;

public class GlobalSetup
{
    [Before(TestDiscovery)]
    public static void BeforeTestDiscovery()
    {
        DiffRunner.Disabled = true;
    }
}
