namespace TUnit.DocTests;

#pragma warning disable CS0169, CS0414

internal abstract class SnippetContext
{
    protected static readonly CancellationToken cancellationToken = default;
    protected static readonly CancellationToken ct = default;
}

public sealed class Program
{
    public static void Main()
    {
    }
}

internal sealed record User(string Name = "Alice");

internal sealed record Person(string Name = "Alice", int Age = 42);
