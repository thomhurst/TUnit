namespace TUnit.TestProject;

[Arguments(1)]
[Arguments(2)]
[ClassDataSource(typeof(TestDataSource))]
public class TestCountVerificationTests(int value)
{
    [Test]
    public async Task SimpleTest()
    {
        await Task.CompletedTask;
    }

    public class TestDataSource
    {
        public static implicit operator int(TestDataSource _) => 10;
    }
}
