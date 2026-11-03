namespace TUnit.TestProject.Bugs._7222;

[Skip("Class-level skip must survive generic resolution failures")]
public class SkippedClassGenerateGenericTestArityTests
{
    [Test, GenerateGenericTest]
    public void EmptyTypeArguments<T>() { }

    [Test, GenerateGenericTest(typeof(int), typeof(string))]
    public void WrongTypeArgumentCount<T>() { }
}
