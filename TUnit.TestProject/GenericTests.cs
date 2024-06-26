using System.Numerics;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public abstract class GenericTests<T>
    where T : INumber<T>
{
    [Test]
    public async Task Test()
    {
        await Assert.That(GetData()).Is.Positive();
    }

    public abstract T GetData();
}

public class GenericInt : GenericTests<int>
{
    public override int GetData()
    {
        return 1;
    }
}

public class GenericDouble : GenericTests<double>
{
    public override double GetData()
    {
        return 1.3;
    }
}