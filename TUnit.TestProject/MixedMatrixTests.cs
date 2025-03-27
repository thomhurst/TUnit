using OneOf;
using TUnit.Assertions;
using TUnit.Assertions.AssertionBuilders.Groups;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class MixedMatrixTests
{
    [Test]
    [MatrixDataSource]
    public Task Render_Theory_Expected(
        Enum1 enum1,
        [Matrix(null, "", "id")] 
        string? id,
        [Matrix(Excluding = [Enum2.Five])]
        Enum2 enum2,
        Enum3 enum3,
        [Matrix(Enum4.One, Enum5.One, "#fff")]
        MixedMatrixTestsUnion1 union1,
        [Matrix(Enum4.One, Enum5.One, "#fff")]
        MixedMatrixTestsUnion2 union2,
        bool rightToLeft
    )
    {
        return Task.CompletedTask;
    }
    
    public enum Enum1
    {
        One
    }
    
    public enum Enum2
    {
        One,
        Two,
        Three,
        Four,
        Five,
    }
    
    public enum Enum3
    {
        One,
        Two,
        Three,
        Four,
    }

    public enum Enum4
    {
        One,
        Two,
        Three,
        Four,
    }

    public enum Enum5
    {
        One,
        Two,
        Three,
        Four,
    }
}

[GenerateOneOf]
public partial class MixedMatrixTestsUnion1 : OneOfBase<MixedMatrixTests.Enum4, MixedMatrixTests.Enum5, string>;
    
[GenerateOneOf]
public partial class MixedMatrixTestsUnion2 : OneOfBase<MixedMatrixTests.Enum4, MixedMatrixTests.Enum5, string>;