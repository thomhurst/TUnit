namespace TUnit.TestProject.Bugs._1924.Keyed;

public class BaseClass
{
    [ClassDataSource<DataClass>(Shared = SharedType.Keyed, Key = "🔑")]
    public required DataClass Data { get; init; }
    
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Repeat(10)]
    public async Task Test(int value)
    {
        await Data.DoSomething();
        await Task.Delay(TimeSpan.FromMilliseconds(50));
    }
}

[InheritsTests]
public class Tests : BaseClass;

[InheritsTests]
public class Tests2 : BaseClass;

[InheritsTests]
public class Tests3 : BaseClass;