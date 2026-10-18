using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

/// <summary>
/// Reflection mode names generic-method instantiations with <see cref="TypeNameFormatter.GetCSharpTypeName"/>.
/// The source generator formats the same names at compile time, so these expectations must match
/// the names in the Tests7105 generator snapshot (#7105).
/// </summary>
public class TestNameTypeFormatterTests
{
    [Test]
    [Arguments(typeof(int), "int")]
    [Arguments(typeof(string), "string")]
    [Arguments(typeof(object), "object")]
    [Arguments(typeof(bool), "bool")]
    [Arguments(typeof(long), "long")]
    [Arguments(typeof(float), "float")]
    [Arguments(typeof(decimal), "decimal")]
    [Arguments(typeof(nint), "nint")]
    [Arguments(typeof(DayOfWeek), "DayOfWeek")]
    [Arguments(typeof(int?), "int?")]
    [Arguments(typeof(int[]), "int[]")]
    [Arguments(typeof(int[][]), "int[][]")]
    [Arguments(typeof(string[,]), "string[,]")]
    [Arguments(typeof(List<int>), "List<int>")]
    [Arguments(typeof(Dictionary<string, List<int?>>), "Dictionary<string, List<int?>>")]
    [Arguments(typeof((int, string)), "(int, string)")]
    [Arguments(typeof(Outer.Inner), "TestNameTypeFormatterTests.Outer.Inner")]
    [Arguments(typeof(Outer.GenericInner<int>), "TestNameTypeFormatterTests.Outer.GenericInner<int>")]
    [Arguments(typeof(GenericOuter<string>.Inner<int>), "TestNameTypeFormatterTests.GenericOuter<string>.Inner<int>")]
    public async Task Formats_Types_As_CSharp_Source(Type type, string expected)
    {
        await Assert.That(TypeNameFormatter.GetCSharpTypeName(type)).IsEqualTo(expected);
    }

    [Test]
    public async Task Appends_Type_Arguments_To_Test_Name()
    {
        var name = TypeNameFormatter.AppendGenericArguments("Method", [typeof(long), typeof(string)]);

        await Assert.That(name).IsEqualTo("Method<long, string>");
    }

    [Test]
    public async Task Leaves_Name_Unchanged_Without_Type_Arguments()
    {
        await Assert.That(TypeNameFormatter.AppendGenericArguments("Method", [])).IsEqualTo("Method");
    }

    [Test]
    [Arguments(typeof(List<int>), "List")]
    [Arguments(typeof(Dictionary<,>), "Dictionary")]
    [Arguments(typeof(string), "String")]
    public async Task Strips_Generic_Arity_From_Class_Name(Type type, string expected)
    {
        await Assert.That(TypeNameFormatter.GetNameWithoutGenericArity(type)).IsEqualTo(expected);
    }

    public class Outer
    {
        public class Inner;

        public class GenericInner<T>;
    }

    public class GenericOuter<T>
    {
        public class Inner<TInner>;
    }
}
