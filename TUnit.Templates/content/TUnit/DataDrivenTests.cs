namespace TestProject;

public class DataDrivenTests
{
    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(5, -3, 2)]
    [Arguments(0, 0, 0)]
    public async Task Add_WithArguments(int a, int b, int expected)
    {
        var calculator = new Calculator();

        var result = calculator.Add(a, b);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [MethodDataSource(nameof(SubtractCases))]
    public async Task Subtract_WithMethodDataSource(int a, int b, int expected)
    {
        var calculator = new Calculator();

        var result = calculator.Subtract(a, b);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [AdditionDataGenerator]
    public async Task Add_WithCustomDataGenerator(int a, int b, int expected)
    {
        var calculator = new Calculator();

        var result = calculator.Add(a, b);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [MatrixDataSource]
    public async Task Multiply_AllCombinations(
        [Matrix(1, 2, 3)] int a,
        [Matrix(0, 1, -1)] int b)
    {
        var calculator = new Calculator();

        var result = calculator.Multiply(a, b);

        await Assert.That(result).IsEqualTo(a * b);
    }

    public static IEnumerable<(int, int, int)> SubtractCases()
    {
        yield return (5, 3, 2);
        yield return (10, 7, 3);
        yield return (0, 0, 0);
    }
}
