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
