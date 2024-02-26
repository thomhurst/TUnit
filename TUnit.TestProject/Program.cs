using TUnit.Engine;

namespace TUnit.TestProject;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await TUnitRunner.RunTests(args);
    }
}