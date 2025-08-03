#pragma warning disable

namespace TUnit.TestProject.OrderedSetupTests;

public class Tests : Base2
{
    [Test]
    public void MyTest()
    {
        Console.WriteLine(@"Test Body");
    }

    [After(Class)]
    public static async Task AssertOrder(ClassHookContext classHookContext)
    {
        await Assert.That(classHookContext.Tests.Single().GetStandardOutput()).IsEqualTo("""
                                                                                         Tests.Z_Before3
                                                                                         Tests.Y_Before3
                                                                                         Tests.A_Before3
                                                                                         Tests.B_Before3
                                                                                         Tests.Z_Before1
                                                                                         Tests.Y_Before1
                                                                                         Tests.A_Before1
                                                                                         Tests.B_Before1
                                                                                         Tests.Z_Before2
                                                                                         Tests.Y_Before2
                                                                                         Tests.A_Before2
                                                                                         Tests.B_Before2
                                                                                         Test Body
                                                                                         Tests.Z_After2
                                                                                         Tests.Y_After2
                                                                                         Tests.A_After2
                                                                                         Tests.B_After2
                                                                                         Tests.Z_After1
                                                                                         Tests.Y_After1
                                                                                         Tests.A_After1
                                                                                         Tests.B_After1
                                                                                         Tests.Z_After3
                                                                                         Tests.Y_After3
                                                                                         Tests.A_After3
                                                                                         Tests.B_After3
                                                                                         """);
    }
}
