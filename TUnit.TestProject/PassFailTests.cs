using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class PassFailTests
{
    [Test]
    [Category("Pass")]
    [EngineTest(ExpectedResult.Pass)]
    public void Pass1()
    {
        // Dummy method
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Category("Pass")]
    [EngineTest(ExpectedResult.Pass)]
    public void Pass2(int value)
    {
        // Dummy method
    }

    [Test]
    [MethodDataSource(nameof(Data1))]
    [MethodDataSource(nameof(Data2))]
    [MethodDataSource(nameof(Data3))]
    [MethodDataSource(nameof(Data4))]
    [MethodDataSource(nameof(Data5))]
    [MethodDataSource(nameof(EnumerableData1))]
    [MethodDataSource(nameof(EnumerableData2))]
    [MethodDataSource(nameof(EnumerableData3))]
    [EngineTest(ExpectedResult.Pass)]
    [Category("Pass")]
    public void Pass3(int value)
    {
        // Dummy method
    }

    [Test]
    [MatrixDataSource]
    [Category("Pass")]
    [EngineTest(ExpectedResult.Pass)]
    public void Pass4(
        [Matrix(1, 2, 3, 4, 5)] int value,
        [Matrix(1, 2, 3, 4)] int value2,
        [Matrix(1, 2, 3)] int value3)
    {
        // Dummy method
    }

    [Test]
    [Category("Fail")]
    [EngineTest(ExpectedResult.Failure)]
    public void Fail1()
    {
        throw new Exception("Test failure");
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Category("Fail")]
    [EngineTest(ExpectedResult.Failure)]
    public void Fail2(int value)
    {
        throw new Exception("Test failure with value: " + value);
    }

    [Test]
    [MethodDataSource(nameof(Data1))]
    [MethodDataSource(nameof(Data2))]
    [MethodDataSource(nameof(Data3))]
    [MethodDataSource(nameof(Data4))]
    [MethodDataSource(nameof(Data5))]
    [MethodDataSource(nameof(EnumerableData1))]
    [MethodDataSource(nameof(EnumerableData2))]
    [MethodDataSource(nameof(EnumerableData3))]
    [Category("Fail")]
    [EngineTest(ExpectedResult.Failure)]
    public void Fail3(int value)
    {
        throw new Exception("Test failure with value: " + value);
    }

    [Test]
    [MatrixDataSource]
    [Category("Fail")]
    [EngineTest(ExpectedResult.Failure)]
    public void Fail4(
        [Matrix(1, 2, 3, 4, 5)] int value,
        [Matrix(1, 2, 3, 4)] int value2,
        [Matrix(1, 2, 3)] int value3)
    {
        throw new Exception("Test failure with values: " + value + ", " + value2 + ", " + value3);
    }

    public static int Data1() => 1;
    public static int Data2() => 1;
    public static int Data3() => 1;
    public static int Data4() => 1;
    public static int Data5() => 1;

    public static IEnumerable<int> EnumerableData1()
    {
        yield return 1;
        yield return 2;
        yield return 3;
        yield return 4;
        yield return 5;
    }

    public static IEnumerable<int> EnumerableData2()
    {
        yield return 6;
        yield return 7;
        yield return 8;
        yield return 9;
        yield return 10;
    }

    public static IEnumerable<int> EnumerableData3()
    {
        yield return 11;
        yield return 12;
        yield return 13;
        yield return 14;
        yield return 15;
    }
}
