using System.Reflection;

namespace TUnit.Engine;

public static class TUnit
{
    public static string OutputDirectory 
        => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
}