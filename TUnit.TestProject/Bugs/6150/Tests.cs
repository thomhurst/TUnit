using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6150;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [MethodDataSource(nameof(Data))]
    public async Task JaggedArray(byte[][] data)
    {
        await Assert.That(data).IsNotNull();
    }

    public static IEnumerable<Func<byte[][]>> Data()
        =>
        [
            static () => [[0, 1, 2, 3, 4, 5, 6, 7, 8, 9]],
            static () => [[0, 1, 2], [3, 4, 5, 6]],
            static () => [[0, 1, 2], [3, 4, 5, 6], [7, 8, 9]]
        ];
}
