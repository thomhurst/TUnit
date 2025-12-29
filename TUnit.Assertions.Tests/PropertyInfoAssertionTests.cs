using System.Reflection;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class PropertyInfoAssertionTests
{
    // Sample class for reflection tests
    private class SampleClass
    {
        public string PublicProperty { get; set; } = "";
        public string ReadOnlyProperty { get; } = "";
        public string WriteOnlyProperty { set { } }
        public static string StaticProperty { get; set; } = "";
        private string PrivateProperty { get; set; } = "";
        public int IntProperty { get; set; }
    }

    private static PropertyInfo GetProperty(string name) =>
        typeof(SampleClass).GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;

    [Test]
    public async Task CanRead_WithReadableProperty_Passes()
    {
        var property = GetProperty("PublicProperty");
        await Assert.That(property).CanRead();
    }

    [Test]
    public async Task CanRead_WithWriteOnlyProperty_Fails()
    {
        var property = GetProperty("WriteOnlyProperty");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(property).CanRead());
    }

    [Test]
    public async Task CannotRead_WithWriteOnlyProperty_Passes()
    {
        var property = GetProperty("WriteOnlyProperty");
        await Assert.That(property).CannotRead();
    }

    [Test]
    public async Task CanWrite_WithWritableProperty_Passes()
    {
        var property = GetProperty("PublicProperty");
        await Assert.That(property).CanWrite();
    }

    [Test]
    public async Task CanWrite_WithReadOnlyProperty_Fails()
    {
        var property = GetProperty("ReadOnlyProperty");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(property).CanWrite());
    }

    [Test]
    public async Task CannotWrite_WithReadOnlyProperty_Passes()
    {
        var property = GetProperty("ReadOnlyProperty");
        await Assert.That(property).CannotWrite();
    }

    [Test]
    public async Task HasGetter_WithGetter_Passes()
    {
        var property = GetProperty("PublicProperty");
        await Assert.That(property).HasGetter();
    }

    [Test]
    public async Task HasGetter_WithoutGetter_Fails()
    {
        var property = GetProperty("WriteOnlyProperty");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(property).HasGetter());
    }

    [Test]
    public async Task HasSetter_WithSetter_Passes()
    {
        var property = GetProperty("PublicProperty");
        await Assert.That(property).HasSetter();
    }

    [Test]
    public async Task HasSetter_WithoutSetter_Fails()
    {
        var property = GetProperty("ReadOnlyProperty");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(property).HasSetter());
    }

    [Test]
    public async Task IsStatic_WithStaticProperty_Passes()
    {
        var property = GetProperty("StaticProperty");
        await Assert.That(property).IsStatic();
    }

    [Test]
    public async Task IsStatic_WithInstanceProperty_Fails()
    {
        var property = GetProperty("PublicProperty");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(property).IsStatic());
    }

    [Test]
    public async Task IsNotStatic_WithInstanceProperty_Passes()
    {
        var property = GetProperty("PublicProperty");
        await Assert.That(property).IsNotStatic();
    }

    [Test]
    public async Task IsPublic_WithPublicProperty_Passes()
    {
        var property = GetProperty("PublicProperty");
        await Assert.That(property).IsPublic();
    }

    [Test]
    public async Task IsPublic_WithPrivateProperty_Fails()
    {
        var property = GetProperty("PrivateProperty");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(property).IsPublic());
    }

    [Test]
    public async Task IsNotPublic_WithPrivateProperty_Passes()
    {
        var property = GetProperty("PrivateProperty");
        await Assert.That(property).IsNotPublic();
    }

    [Test]
    public async Task IsOfType_WithMatchingType_Passes()
    {
        var property = GetProperty("PublicProperty");
        await Assert.That(property).IsOfType(typeof(string));
    }

    [Test]
    public async Task IsOfType_WithMismatchedType_Fails()
    {
        var property = GetProperty("IntProperty");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(property).IsOfType(typeof(string)));
    }

    [Test]
    public async Task HasName_WithMatchingName_Passes()
    {
        var property = GetProperty("PublicProperty");
        await Assert.That(property).HasName("PublicProperty");
    }

    [Test]
    public async Task HasName_WithMismatchedName_Fails()
    {
        var property = GetProperty("PublicProperty");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(property).HasName("OtherProperty"));
    }
}
