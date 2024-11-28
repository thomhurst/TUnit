// ReSharper disable UseCollectionExpression

namespace TUnit.TestProject;

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
        // Dummy method
    }
    
    public static int T() => 1;

    public static Func<int> FuncT() => () => 1;

    public static IEnumerable<int> EnumerableT() => [1];

    public static IEnumerable<Func<int>> EnumerableFuncT() => [() => 1];

    public static IEnumerable<int> ArrayT() => [1];

    public static IEnumerable<Func<int>> ArrayFuncT() => [() => 1];

    public abstract class BaseValue;

    public class ConcreteValue : BaseValue;
}