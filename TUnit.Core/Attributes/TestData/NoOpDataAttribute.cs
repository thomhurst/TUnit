namespace TUnit.Core;

internal class NoOpDataAttribute : IDataAttribute
{
    public static IDataAttribute[] Array { get; } = [ new NoOpDataAttribute() ];
}
