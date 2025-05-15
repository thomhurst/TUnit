namespace TUnit.Core;

public interface IDataAttribute;

internal class NoOpDataAttribute : IDataAttribute
{
    public static IDataAttribute[] Array { get; } = [ new NoOpDataAttribute() ];
}