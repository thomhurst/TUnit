using TUnit.Core;

namespace TUnit.TestProject;

// This class intentionally has compilation errors to test ErrorSymbol handling
public class ErrorSymbolTestClass : NonExistentBaseClass  // This will cause TypeKind.Error
{
    [Test]
    public void TestMethodInErrorClass()
    {
        // This test method should not generate source because the containing class has error symbols
    }
}

// Another class with a different type of error
public class AnotherErrorClass 
{
    public UndefinedType SomeProperty { get; set; }  // This will cause TypeKind.Error in property type
    
    [Test]
    public void AnotherTestMethod()
    {
        // This test method should also not generate source
    }
}