using Shouldly;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Tests;

public class MetadataTypeNameFormatterTests
{
    [Test]
    [Arguments(typeof(string), "System.String")]
    [Arguments(typeof(void), "System.Void")]
    [Arguments(typeof(int[]), "System.Int32[]")]
    [Arguments(typeof(int[][]), "System.Int32[][]")]
    [Arguments(typeof(int[,]), "System.Int32[,]")]
    [Arguments(typeof(List<string>), "System.Collections.Generic.List`1<System.String>")]
    [Arguments(typeof(List<>), "System.Collections.Generic.List`1")]
    [Arguments(typeof(Dictionary<string, List<int>>), "System.Collections.Generic.Dictionary`2<System.String,System.Collections.Generic.List`1<System.Int32>>")]
    [Arguments(typeof(Task<int>), "System.Threading.Tasks.Task`1<System.Int32>")]
    [Arguments(typeof(int?), "System.Nullable`1<System.Int32>")]
    [Arguments(typeof(Outer.Inner), "TUnit.Engine.Tests.MetadataTypeNameFormatterTests+Outer+Inner")]
    [Arguments(typeof(Outer.GenericInner<string>), "TUnit.Engine.Tests.MetadataTypeNameFormatterTests+Outer+GenericInner`1<System.String>")]
    public void Formats_Types_In_Metadata_Format(Type type, string expected)
    {
        MetadataTypeNameFormatter.GetMetadataFullName(type).ShouldBe(expected);
    }

    [Test]
    public void Formats_Generic_Method_Parameter_As_DoubleBang_Position()
    {
        var method = typeof(GenericMembers).GetMethod(nameof(GenericMembers.MethodWithGenericParameters))!;
        var parameters = method.GetParameters();

        MetadataTypeNameFormatter.GetMetadataFullName(parameters[0].ParameterType).ShouldBe("!!0");
        MetadataTypeNameFormatter.GetMetadataFullName(parameters[1].ParameterType).ShouldBe("!!1[]");
        MetadataTypeNameFormatter.GetMetadataFullName(parameters[2].ParameterType).ShouldBe("System.Collections.Generic.List`1<!!0>");
    }

    [Test]
    public void Formats_Generic_Type_Parameter_As_SingleBang_Position()
    {
        var typeParameter = typeof(Dictionary<,>).GetGenericArguments()[1];

        MetadataTypeNameFormatter.GetMetadataFullName(typeParameter).ShouldBe("!1");
    }

    [Test]
    public void Formats_ByRef_Parameter_With_Ampersand()
    {
        var method = typeof(GenericMembers).GetMethod(nameof(GenericMembers.MethodWithRefParameter))!;
        var parameterType = method.GetParameters()[0].ParameterType;

        MetadataTypeNameFormatter.GetMetadataFullName(parameterType).ShouldBe("System.Int32&");
    }

    public static class Outer
    {
        public class Inner;

        public class GenericInner<T>;
    }

    public static class GenericMembers
    {
        public static void MethodWithGenericParameters<T1, T2>(T1 first, T2[] second, List<T1> third)
        {
        }

        public static void MethodWithRefParameter(ref int value)
        {
        }
    }
}
