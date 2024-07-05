namespace TUnit.Core;

public record TestData(object? Argument, Type Type, InjectedDataType InjectedDataType)
{
    public string? StringKey { get; init; }
}