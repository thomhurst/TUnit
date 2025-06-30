// ReSharper disable UseCollectionExpression

using System.Diagnostics.CodeAnalysis;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[UnconditionalSuppressMessage("Usage", "TUnit0046:Return a `Func<T>` rather than a `<T>`")]
public class MethodDataSourceDrivenWithCancellationTokenTests
{
    [Test]
    [MethodDataSource(nameof(T))]
    [MethodDataSource(nameof(FuncT))]
    [MethodDataSource(nameof(EnumerableT))]
    [MethodDataSource(nameof(EnumerableFuncT))]
    [MethodDataSource(nameof(ArrayT))]
    [MethodDataSource(nameof(ArrayFuncT))]
    public void MyTest(int value, CancellationToken cancellationToken)
    {
        Console.WriteLine(value);
    }

    public static int T() => 1;

    public static Func<int> FuncT() => () => 1;

    public static IEnumerable<int> EnumerableT() => [1];

    public static IEnumerable<Func<int>> EnumerableFuncT() => [() => 1];

    public static int[] ArrayT() => [1];

    public static Func<int>[] ArrayFuncT() => [() => 1];
}
