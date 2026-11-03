namespace TUnit.TestProject.Bugs._7222;

public class ExplicitGenerateGenericTestArityTests
{
    [Test]
    public void NormalTest()
    {
    }

    [Test, Explicit, GenerateGenericTest]
    public void EmptyTypeArguments<T>()
    {
    }

    [Test, Explicit, GenerateGenericTest(typeof(int), typeof(string))]
    public void WrongTypeArgumentCount<T>()
    {
    }
}
