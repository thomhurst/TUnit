using System.Numerics;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public abstract class GenericTests<T>
    where T : INumber<T>
{
    [Test]
    public async Task Test()
    {
        await Assert.That(GetData()).IsPositive();
    }

    [Test]
    public abstract Task DataDrivenTest(T t);

    [Test]
    [MethodDataSource(nameof(GetData))]
    [MethodDataSource(nameof(GetEnumerableData))]
    public async Task DataSourceDrivenTest(T t)
    {
        await Task.CompletedTask;
    }
    
    public abstract T GetData();
    
    public abstract IEnumerable<T> GetEnumerableData();
}

[InheritsTests]
public class GenericInt : GenericTests<int>
{
    public override int GetData()
    {
        return 1;
    }

    public override IEnumerable<int> GetEnumerableData()
    {
        yield return 1;
        yield return 2;
        yield return 3;
        yield return 4;
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    public override async Task DataDrivenTest(int t)
    {
        await Task.CompletedTask;
    }
}

[InheritsTests]
public class GenericDouble : GenericTests<double>
{
    [Test]
    [Arguments(1.7)]
    [Arguments(2.7)]
    [Arguments(3.7)]
    [Arguments(4.7)]
    public override async Task DataDrivenTest(double t)
    {
        await Task.CompletedTask;
    }

    public override double GetData()
    {
        return 1.3;
    }

    public override IEnumerable<double> GetEnumerableData()
    {
        yield return 1.4;
        yield return 2.4;
        yield return 3.4;
        yield return 4.4;
    }
}