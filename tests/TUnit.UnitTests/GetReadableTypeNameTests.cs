#if NET
using TUnit.Core;

namespace TUnit.UnitTests;

public class GetReadableTypeNameTests
{
    [Test]
    public async Task Simple_Type_Returns_Name()
    {
        var name = TUnitActivitySource.GetReadableTypeName(typeof(string));
        await Assert.That(name).IsEqualTo("String");
    }

    [Test]
    public async Task Nested_Type_Uses_Dot_Separator()
    {
        var name = TUnitActivitySource.GetReadableTypeName(typeof(OuterClass.InnerClass));
        await Assert.That(name).IsEqualTo("OuterClass.InnerClass");
    }

    [Test]
    public async Task Generic_Type_Strips_Arity_Suffix()
    {
        var name = TUnitActivitySource.GetReadableTypeName(typeof(List<int>));
        await Assert.That(name).IsEqualTo("List");
    }

    [Test]
    public async Task Deeply_Nested_Type_Preserves_Chain()
    {
        var name = TUnitActivitySource.GetReadableTypeName(typeof(OuterClass.InnerClass.DeeplyNested));
        await Assert.That(name).IsEqualTo("OuterClass.InnerClass.DeeplyNested");
    }
}

// Test fixtures for nested type tests
public class OuterClass
{
    public class InnerClass
    {
        public class DeeplyNested;
    }
}
#endif
