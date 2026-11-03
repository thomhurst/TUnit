namespace TUnit.TestProject.Bugs._7445;

public sealed class SingleUseResource : IDisposable
{
    private int _used;

    public void Use()
    {
        if (Interlocked.Exchange(ref _used, 1) != 0)
        {
            throw new InvalidOperationException("A factory value was reused after use or disposal.");
        }
    }

    public void Dispose() => Interlocked.Exchange(ref _used, 1);
}

public static class FactoryRows
{
    public static IEnumerable<Func<SingleUseResource>> Resources() => [() => new SingleUseResource()];
}

[GenerateGenericTest(typeof(SingleUseResource))]
[MethodDataSource(typeof(FactoryRows), nameof(FactoryRows.Resources))]
public class FreshFactoryRows<T>(T value) : IDisposable
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [NotInParallel]
    public void Fresh_Value_For_Each_Method_Row(int row)
    {
        _ = row;
        ((SingleUseResource)(object)value!).Use();
    }

    public void Dispose() => ((IDisposable)value!).Dispose();
}
