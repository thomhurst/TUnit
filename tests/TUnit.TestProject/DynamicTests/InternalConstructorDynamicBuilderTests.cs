using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.DynamicTests;

// The generated wrapper can construct this builder from within the same assembly.
internal class InternalConstructorDynamicBuilder
{
    internal InternalConstructorDynamicBuilder()
    {
    }

#pragma warning disable TUnitWIP0001
    [DynamicTestBuilder]
#pragma warning restore TUnitWIP0001
    public void BuildTests(DynamicTestBuilderContext context)
    {
        context.AddTest(new DynamicTest<InternalConstructorDynamicBuilderTests>
        {
            TestMethod = test => test.TestFromInternalBuilder(),
            TestMethodArguments = [],
            Attributes = []
        });
    }
}

[EngineTest(ExpectedResult.Pass)]
public class InternalConstructorDynamicBuilderTests
{
    // Discovery must construct the builder, whose constructor is internal,
    // independently of this publicly constructible test class.
    public async Task TestFromInternalBuilder()
    {
        await Assert.That(TestContext.Current!.Metadata.TestDetails.TestLineNumber).IsGreaterThan(0);
    }
}
