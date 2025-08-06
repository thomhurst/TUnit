namespace TUnit.Assertions.Tests.Bugs;

public class Tests1774
{
    [Test]
    public async Task Test()
    {
        Type1 type1 = new Type2();

        await Assert.That(() => type1)
            .ThrowsNothing()
            .And
            .IsTypeOf<Type2>()
            .And
            .Satisfies(res => res?.Property2, assert => assert.IsNotNullOrEmpty()!);
    }

    public record Type1
    {
        public string Property1 => "Value1";
    }

    public record Type2 : Type1
    {
        public string Property2 => "Value2";
    }
}
