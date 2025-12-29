using System.Reflection;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class MethodInfoAssertionTests
{
    // Sample class for reflection tests
    private class SampleClass
    {
        public void PublicMethod() { }
        private void PrivateMethod() { }
        public static void StaticMethod() { }
        public virtual void VirtualMethod() { }
        protected void ProtectedMethod() { }
        internal void InternalMethod() { }

        public string MethodWithReturn() => "";
        public void MethodWithParams(int a, string b, bool c) { }
    }

    private abstract class AbstractSampleClass
    {
        public abstract void AbstractMethod();
    }

    private static MethodInfo GetMethod(string name) =>
        typeof(SampleClass).GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;

    [Test]
    public async Task IsPublic_WithPublicMethod_Passes()
    {
        var method = GetMethod("PublicMethod");
        await Assert.That(method).IsPublic();
    }

    [Test]
    public async Task IsPublic_WithPrivateMethod_Fails()
    {
        var method = GetMethod("PrivateMethod");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(method).IsPublic());
    }

    [Test]
    public async Task IsNotPublic_WithPrivateMethod_Passes()
    {
        var method = GetMethod("PrivateMethod");
        await Assert.That(method).IsNotPublic();
    }

    [Test]
    public async Task IsPrivate_WithPrivateMethod_Passes()
    {
        var method = GetMethod("PrivateMethod");
        await Assert.That(method).IsPrivate();
    }

    [Test]
    public async Task IsStatic_WithStaticMethod_Passes()
    {
        var method = GetMethod("StaticMethod");
        await Assert.That(method).IsStatic();
    }

    [Test]
    public async Task IsStatic_WithInstanceMethod_Fails()
    {
        var method = GetMethod("PublicMethod");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(method).IsStatic());
    }

    [Test]
    public async Task IsNotStatic_WithInstanceMethod_Passes()
    {
        var method = GetMethod("PublicMethod");
        await Assert.That(method).IsNotStatic();
    }

    [Test]
    public async Task IsVirtual_WithVirtualMethod_Passes()
    {
        var method = GetMethod("VirtualMethod");
        await Assert.That(method).IsVirtual();
    }

    [Test]
    public async Task IsVirtual_WithNonVirtualMethod_Fails()
    {
        var method = GetMethod("PublicMethod");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(method).IsVirtual());
    }

    [Test]
    public async Task IsAbstract_WithAbstractMethod_Passes()
    {
        var method = typeof(AbstractSampleClass).GetMethod("AbstractMethod")!;
        await Assert.That(method).IsAbstract();
    }

    [Test]
    public async Task IsProtected_WithProtectedMethod_Passes()
    {
        var method = GetMethod("ProtectedMethod");
        await Assert.That(method).IsProtected();
    }

    [Test]
    public async Task IsInternal_WithInternalMethod_Passes()
    {
        var method = GetMethod("InternalMethod");
        await Assert.That(method).IsInternal();
    }

    [Test]
    public async Task ReturnsType_WithMatchingReturnType_Passes()
    {
        var method = GetMethod("MethodWithReturn");
        await Assert.That(method).ReturnsType(typeof(string));
    }

    [Test]
    public async Task ReturnsType_WithMismatchedReturnType_Fails()
    {
        var method = GetMethod("MethodWithReturn");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(method).ReturnsType(typeof(int)));
    }

    [Test]
    public async Task HasParameterCount_WithMatchingCount_Passes()
    {
        var method = GetMethod("MethodWithParams");
        await Assert.That(method).HasParameterCount(3);
    }

    [Test]
    public async Task HasParameterCount_WithMismatchedCount_Fails()
    {
        var method = GetMethod("MethodWithParams");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(method).HasParameterCount(2));
    }

    [Test]
    public async Task HasParameterCount_WithNoParams_Passes()
    {
        var method = GetMethod("PublicMethod");
        await Assert.That(method).HasParameterCount(0);
    }

    [Test]
    public async Task HasName_WithMatchingName_Passes()
    {
        var method = GetMethod("PublicMethod");
        await Assert.That(method).HasName("PublicMethod");
    }

    [Test]
    public async Task HasName_WithMismatchedName_Fails()
    {
        var method = GetMethod("PublicMethod");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(method).HasName("OtherMethod"));
    }
}
