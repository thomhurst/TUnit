using System.Numerics;
using TUnit.Assertions;
using TUnit.Assertions.Extensions.Numbers;
using TUnit.Core;

namespace TUnit.TestProject;

[ClassDataSource<int>]
[ClassDataSource<double>]
[ClassDataSource<float>]
public class GenericTests2<T> where T : INumber<T>
{
    public GenericTests2(T t)
    {
    }
    
    [Test]
    public Task Test()
    {
        return Task.CompletedTask;
    }
}