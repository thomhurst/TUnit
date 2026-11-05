using System.Reflection;
using TUnit.Core.Extensions;

namespace TUnit.UnitTests;

public class AttributeInheritanceTests
{
    [Test]
    [Arguments(typeof(Derived), new[] { "derived", "base" })]
    [Arguments(typeof(ImplicitDerived), new[] { "derived" })]
    public async Task Framework_fallback_matches_runtime_inheritance(Type type, string[] expected)
    {
        // Exercise the NETSTANDARD implementation on every runtime, including CI's net472 leg.
        var fallback = typeof(ReflectionExtensions).GetMethod("GetAttributesViaCustomAttributeData",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var actual = (Attribute[])fallback.Invoke(null, [type, typeof(Attribute), true])!;
#if NET
        await Assert.That(string.Join(",", type.GetCustomAttributes(true).OfType<LabelAttribute>().Select(x => x.Label)))
            .IsEqualTo(string.Join(",", expected));
#endif
        await Assert.That(string.Join(",", actual.OfType<LabelAttribute>().Select(x => x.Label)))
            .IsEqualTo(string.Join(",", expected));
    }

    [Test]
    [Arguments(typeof(MethodDerived), true, "derived,middle-repeat,base,base-repeat")]
    [Arguments(typeof(MethodDerived), false, "derived")]
    [Arguments(typeof(MethodHidden), true, "hidden")]
    [Arguments(typeof(GenericMethodDerived), true, "generic-derived,generic-base")]
    public async Task Framework_fallback_matches_overridden_method_inheritance(Type type, bool inherit, string expected)
    {
        var method = type.GetMethod("Run", [typeof(int)])!;
        var fallback = typeof(ReflectionExtensions).GetMethod("GetAttributesViaCustomAttributeData",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var actual = (Attribute[])fallback.Invoke(null, [method, typeof(Attribute), inherit])!;
#if NET
        await Assert.That(string.Join(",", method.GetCustomAttributes(inherit).OfType<LabelAttribute>().Select(x => x.Label)))
            .IsEqualTo(expected);
#endif
        await Assert.That(string.Join(",", actual.OfType<LabelAttribute>().Select(x => x.Label)))
            .IsEqualTo(expected);
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    private sealed class MethodSingleAttribute<T>(string label) : LabelAttribute(label);

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    private sealed class MethodRepeatAttribute(string label) : LabelAttribute(label);

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    private sealed class MethodLocalAttribute() : LabelAttribute("excluded");

    public class MethodBase
    {
        [MethodSingle<string>("base"), MethodRepeat("base-repeat"), MethodLocal]
        public virtual void Run(int value) { }

        [MethodRepeat("wrong-overload")]
        public virtual void Run(string value) { }
    }

    public class MethodMiddle : MethodBase
    {
        [MethodSingle<int>("middle"), MethodRepeat("middle-repeat")]
        public override void Run(int value) { }
    }

    public class MethodDerived : MethodMiddle
    {
        [MethodSingle<int>("derived")]
        public override void Run(int value) { }
    }

    public class MethodHidden : MethodBase
    {
        [MethodRepeat("hidden")]
        public new virtual void Run(int value) { }
    }

    public class GenericMethodBase<T>
    {
        [MethodRepeat("generic-base")]
        public virtual void Run(T value) { }
    }

    public class GenericMethodDerived : GenericMethodBase<int>
    {
        [MethodRepeat("generic-derived")]
        public override void Run(int value) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class LabelAttribute(string label) : Attribute
    {
        public string Label { get; } = label;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    private sealed class SingleAttribute<T>(string label) : LabelAttribute(label);

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    private sealed class LocalAttribute() : LabelAttribute("excluded");

    private sealed class ImplicitAttribute(string label) : LabelAttribute(label);

    [Single<string>("base")]
    [Local]
    public class Base;
    [Single<int>("middle")]
    public class Middle : Base;
    [Single<int>("derived")]
    public class Derived : Middle;

    [Implicit("base")]
    public class ImplicitBase;
    [Implicit("derived")]
    public class ImplicitDerived : ImplicitBase;
}
