using System.Reflection;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class FieldInfoAssertionTests
{
    // Sample class for reflection tests
    private class SampleClass
    {
        public string PublicField = "";
        private string PrivateField = "";
        public static string StaticField = "";
        public readonly string ReadOnlyField = "";
        public const string ConstField = "const";
        protected string ProtectedField = "";
        internal string InternalField = "";
        public int IntField = 0;
    }

    private static FieldInfo GetField(string name) =>
        typeof(SampleClass).GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;

    [Test]
    public async Task IsPublic_WithPublicField_Passes()
    {
        var field = GetField("PublicField");
        await Assert.That(field).IsPublic();
    }

    [Test]
    public async Task IsPublic_WithPrivateField_Fails()
    {
        var field = GetField("PrivateField");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(field).IsPublic());
    }

    [Test]
    public async Task IsNotPublic_WithPrivateField_Passes()
    {
        var field = GetField("PrivateField");
        await Assert.That(field).IsNotPublic();
    }

    [Test]
    public async Task IsPrivate_WithPrivateField_Passes()
    {
        var field = GetField("PrivateField");
        await Assert.That(field).IsPrivate();
    }

    [Test]
    public async Task IsStatic_WithStaticField_Passes()
    {
        var field = GetField("StaticField");
        await Assert.That(field).IsStatic();
    }

    [Test]
    public async Task IsStatic_WithInstanceField_Fails()
    {
        var field = GetField("PublicField");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(field).IsStatic());
    }

    [Test]
    public async Task IsNotStatic_WithInstanceField_Passes()
    {
        var field = GetField("PublicField");
        await Assert.That(field).IsNotStatic();
    }

    [Test]
    public async Task IsReadOnly_WithReadOnlyField_Passes()
    {
        var field = GetField("ReadOnlyField");
        await Assert.That(field).IsReadOnly();
    }

    [Test]
    public async Task IsReadOnly_WithNonReadOnlyField_Fails()
    {
        var field = GetField("PublicField");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(field).IsReadOnly());
    }

    [Test]
    public async Task IsNotReadOnly_WithNonReadOnlyField_Passes()
    {
        var field = GetField("PublicField");
        await Assert.That(field).IsNotReadOnly();
    }

    [Test]
    public async Task IsConstant_WithConstField_Passes()
    {
        var field = GetField("ConstField");
        await Assert.That(field).IsConstant();
    }

    [Test]
    public async Task IsConstant_WithNonConstField_Fails()
    {
        var field = GetField("PublicField");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(field).IsConstant());
    }

    [Test]
    public async Task IsNotConstant_WithNonConstField_Passes()
    {
        var field = GetField("PublicField");
        await Assert.That(field).IsNotConstant();
    }

    [Test]
    public async Task IsProtected_WithProtectedField_Passes()
    {
        var field = GetField("ProtectedField");
        await Assert.That(field).IsProtected();
    }

    [Test]
    public async Task IsInternal_WithInternalField_Passes()
    {
        var field = GetField("InternalField");
        await Assert.That(field).IsInternal();
    }

    [Test]
    public async Task IsOfType_WithMatchingType_Passes()
    {
        var field = GetField("PublicField");
        await Assert.That(field).IsOfType(typeof(string));
    }

    [Test]
    public async Task IsOfType_WithMismatchedType_Fails()
    {
        var field = GetField("IntField");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(field).IsOfType(typeof(string)));
    }

    [Test]
    public async Task HasName_WithMatchingName_Passes()
    {
        var field = GetField("PublicField");
        await Assert.That(field).HasName("PublicField");
    }

    [Test]
    public async Task HasName_WithMismatchedName_Fails()
    {
        var field = GetField("PublicField");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(field).HasName("OtherField"));
    }
}
