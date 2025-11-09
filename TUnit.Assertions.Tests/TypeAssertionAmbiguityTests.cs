namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests to ensure no ambiguous invocation errors for type assertion methods.
/// Regression tests for GitHub issue #3737.
/// </summary>
public class TypeAssertionAmbiguityTests
{
    // Test hierarchy for inheritance testing
    private interface IElement { }
    private class Element : IElement { }
    private class DerivedElement : Element { }

    // ============ IsTypeOf TESTS ============

    [Test]
    public async Task IsTypeOf_SingleTypeParameter_NoAmbiguity()
    {
        object obj = "test";
        var result = await Assert.That(obj).IsTypeOf<string>();
        await Assert.That(result).IsEqualTo("test");
    }

    [Test]
    public async Task IsTypeOf_WithBaseType_NoAmbiguity()
    {
        IElement element = new Element();
        var result = await Assert.That(element).IsTypeOf<Element>();
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task IsTypeOf_WithDerivedType_NoAmbiguity()
    {
        Element element = new DerivedElement();
        var result = await Assert.That(element).IsTypeOf<DerivedElement>();
        await Assert.That(result).IsNotNull();
    }

    // ============ IsNotTypeOf TESTS ============

    [Test]
    public async Task IsNotTypeOf_SingleTypeParameter_NoAmbiguity()
    {
        object obj = "test";
        await Assert.That(obj).IsNotTypeOf<int>();
    }

    [Test]
    public async Task IsNotTypeOf_WithBaseType_NoAmbiguity()
    {
        IElement element = new Element();
        await Assert.That(element).IsNotTypeOf<DerivedElement>();
    }

    [Test]
    public async Task IsNotTypeOf_WithDerivedType_NoAmbiguity()
    {
        Element element = new Element();
        await Assert.That(element).IsNotTypeOf<DerivedElement>();
    }

    [Test]
    public async Task IsNotTypeOf_Fails_WhenTypeMatches()
    {
        await Assert.That(async () =>
        {
            object obj = "test";
            await Assert.That(obj).IsNotTypeOf<string>();
        }).Throws<TUnit.Assertions.Exceptions.AssertionException>();
    }

    // ============ IsAssignableTo TESTS ============

    [Test]
    public async Task IsAssignableTo_SingleTypeParameter_NoAmbiguity()
    {
        Element element = new Element();
        await Assert.That(element).IsAssignableTo<IElement>();
    }

    [Test]
    public async Task IsAssignableTo_DerivedToBase_NoAmbiguity()
    {
        DerivedElement derived = new DerivedElement();
        await Assert.That(derived).IsAssignableTo<Element>();
    }

    [Test]
    public async Task IsAssignableTo_DerivedToInterface_NoAmbiguity()
    {
        DerivedElement derived = new DerivedElement();
        await Assert.That(derived).IsAssignableTo<IElement>();
    }

    [Test]
    public async Task IsAssignableTo_ObjectVariable_NoAmbiguity()
    {
        object obj = new Element();
        await Assert.That(obj).IsAssignableTo<IElement>();
    }

    [Test]
    public async Task IsAssignableTo_ExactType_NoAmbiguity()
    {
        Element element = new Element();
        await Assert.That(element).IsAssignableTo<Element>();
    }

    [Test]
    public async Task IsAssignableTo_Fails_WhenNotAssignable()
    {
        await Assert.That(async () =>
        {
            Element element = new Element();
            await Assert.That(element).IsAssignableTo<DerivedElement>();
        }).Throws<TUnit.Assertions.Exceptions.AssertionException>();
    }

    // ============ IsNotAssignableTo TESTS ============

    [Test]
    public async Task IsNotAssignableTo_SingleTypeParameter_NoAmbiguity()
    {
        Element element = new Element();
        await Assert.That(element).IsNotAssignableTo<DerivedElement>();
    }

    [Test]
    public async Task IsNotAssignableTo_UnrelatedTypes_NoAmbiguity()
    {
        Element element = new Element();
        await Assert.That(element).IsNotAssignableTo<string>();
    }

    [Test]
    public async Task IsNotAssignableTo_ObjectVariable_NoAmbiguity()
    {
        object obj = new Element();
        await Assert.That(obj).IsNotAssignableTo<string>();
    }

    [Test]
    public async Task IsNotAssignableTo_Fails_WhenAssignable()
    {
        await Assert.That(async () =>
        {
            Element element = new Element();
            await Assert.That(element).IsNotAssignableTo<IElement>();
        }).Throws<TUnit.Assertions.Exceptions.AssertionException>();
    }

    // ============ ISSUE #3737 REGRESSION TESTS ============

    [Test]
    public async Task Issue3737_IsAssignableTo_WithInterface_NoAmbiguity()
    {
        Element element = new Element();
        await Assert.That(element).IsAssignableTo<IElement>();
    }

    [Test]
    public async Task Issue3737_IsAssignableTo_WithBaseClass_NoAmbiguity()
    {
        DerivedElement derived = new DerivedElement();
        await Assert.That(derived).IsAssignableTo<Element>();
    }

    [Test]
    public async Task Issue3737_IsNotAssignableTo_WithUnrelatedType_NoAmbiguity()
    {
        Element element = new Element();
        await Assert.That(element).IsNotAssignableTo<string>();
    }

    // ============ CHAINING TESTS ============

    [Test]
    public async Task IsTypeOf_Chained_NoAmbiguity()
    {
        object obj = "test";
        await Assert.That(obj)
            .IsNotNull()
            .And
            .IsTypeOf<string>()
            .And
            .HasLength(4);
    }

    [Test]
    public async Task IsNotTypeOf_Chained_NoAmbiguity()
    {
        object obj = "test";
        await Assert.That(obj)
            .IsNotNull()
            .And
            .IsNotTypeOf<int>()
            .And
            .IsTypeOf<string>();
    }

    [Test]
    public async Task IsAssignableTo_Chained_NoAmbiguity()
    {
        Element element = new Element();
        await Assert.That(element)
            .IsNotNull()
            .And
            .IsAssignableTo<IElement>()
            .And
            .IsAssignableTo<Element>();
    }

    [Test]
    public async Task IsNotAssignableTo_Chained_NoAmbiguity()
    {
        Element element = new Element();
        await Assert.That(element)
            .IsNotNull()
            .And
            .IsNotAssignableTo<DerivedElement>()
            .And
            .IsNotAssignableTo<string>();
    }

    // ============ GENERIC TYPE TESTS ============

    [Test]
    public async Task IsTypeOf_GenericType_NoAmbiguity()
    {
        object obj = new List<string> { "a", "b" };
        var result = await Assert.That(obj).IsTypeOf<List<string>>();
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task IsNotTypeOf_GenericType_NoAmbiguity()
    {
        object obj = new List<string>();
        await Assert.That(obj).IsNotTypeOf<List<int>>();
    }

    [Test]
    public async Task IsAssignableTo_GenericInterface_NoAmbiguity()
    {
        List<string> list = new List<string>();
        await Assert.That(list).IsAssignableTo<IEnumerable<string>>();
    }

    [Test]
    public async Task IsNotAssignableTo_WrongGenericType_NoAmbiguity()
    {
        List<string> list = new List<string>();
        await Assert.That(list).IsNotAssignableTo<IEnumerable<int>>();
    }

    // ============ VALUE TYPE TESTS ============

    [Test]
    public async Task IsTypeOf_ValueType_NoAmbiguity()
    {
        object boxed = 42;
        var result = await Assert.That(boxed).IsTypeOf<int>();
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task IsNotTypeOf_ValueType_NoAmbiguity()
    {
        object boxed = 42;
        await Assert.That(boxed).IsNotTypeOf<long>();
    }

    [Test]
    public async Task IsAssignableTo_ValueType_NoAmbiguity()
    {
        object boxed = 42;
        await Assert.That(boxed).IsAssignableTo<IComparable>();
    }

    [Test]
    public async Task IsNotAssignableTo_ValueType_NoAmbiguity()
    {
        object boxed = 42;
        await Assert.That(boxed).IsNotAssignableTo<string>();
    }

    // ============ ALL FOUR METHODS TOGETHER ============

    [Test]
    public async Task AllFourMethods_Combined_NoAmbiguity()
    {
        object obj1 = new Element();
        await Assert.That(obj1).IsTypeOf<Element>();

        object obj2 = "test";
        await Assert.That(obj2).IsNotTypeOf<int>();

        Element element = new Element();
        await Assert.That(element).IsAssignableTo<IElement>();

        Element element2 = new Element();
        await Assert.That(element2).IsNotAssignableTo<DerivedElement>();
    }

    [Test]
    public async Task AllFourMethods_InSingleChain_NoAmbiguity()
    {
        object obj = new Element();

        await Assert.That(obj)
            .IsNotNull()
            .And
            .IsNotTypeOf<string>()
            .And
            .IsTypeOf<Element>()
            .And
            .IsAssignableTo<IElement>()
            .And
            .IsNotAssignableTo<DerivedElement>();
    }
}
