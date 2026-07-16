using TUnit.TestProject.Attributes;

#pragma warning disable TUnitWIP0001

namespace TUnit.TestProject.Bugs._3173;

[EngineTest(ExpectedResult.Pass)]
public class DynamicTestArgumentsTests
{
    [DynamicTestBuilder]
    public void BuildDynamicTests(DynamicTestBuilderContext context)
    {
        string testValue = "abc";

        context.AddTest(new DynamicTest<DynamicTestArgumentsTests>
        {
            TestMethod = (@class) => @class.TestStringArgument(DynamicTestHelper.Argument<string>()),
            TestMethodArguments = [testValue],
        });

        int testInt = 42;
        context.AddTest(new DynamicTest<DynamicTestArgumentsTests>
        {
            TestMethod = (@class) => @class.TestIntArgument(DynamicTestHelper.Argument<int>()),
            TestMethodArguments = [testInt],
        });

        context.AddTest(new DynamicTest<DynamicTestArgumentsTests>
        {
            TestMethod = (@class) => @class.TestMultipleArguments(
                DynamicTestHelper.Argument<string>(),
                DynamicTestHelper.Argument<int>(),
                DynamicTestHelper.Argument<bool>()),
            TestMethodArguments = ["test", 123, true],
        });
    }

    public async Task TestStringArgument(string value)
    {
        await Assert.That(value).IsNotNull();
        await Assert.That(value).IsEqualTo("abc");
    }

    public async Task TestIntArgument(int value)
    {
        await Assert.That(value).IsNotEqualTo(0);
        await Assert.That(value).IsEqualTo(42);
    }

    public async Task TestMultipleArguments(string str, int num, bool flag)
    {
        await Assert.That(str).IsEqualTo("test");
        await Assert.That(num).IsEqualTo(123);
        await Assert.That(flag).IsTrue();
    }
}
