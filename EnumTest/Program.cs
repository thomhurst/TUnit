using EnumDefintion;
using FluentAssertions;

namespace EnumTest;

public class EnumTest
{
    [Test]
    [Arguments(MyEnum.Foo)]
    [Arguments(MyEnum.Bar)]
    public void MyEnumTest(MyEnum myEnum)
    {
        myEnum.Should().Be(MyEnum.Foo);
    }
}