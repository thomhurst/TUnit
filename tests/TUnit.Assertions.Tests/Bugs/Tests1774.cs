namespace TUnit.Assertions.Tests.Bugs;

public class Tests1774
{
    // [Test]
    // [Skip("Extension method resolution issues with Polyfill package")]
    // public async Task Test()
    // {
    //     Type1 type1 = new Type2();
    //
    //     var result = await Assert.That(() => type1).ThrowsNothing();
    //     await Assert.That((object)result)
    //         .IsTypeOf<Type2>()
    //         .And
    //         .Satisfies(res => res?.Property2, assert => assert.IsNotNullOrEmpty()!);
    // }

    public record Type1
    {
        public string Property1 => "Value1";
    }

    public record Type2 : Type1
    {
        public string Property2 => "Value2";
    }
}
