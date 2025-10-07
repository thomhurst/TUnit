using System.Text;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class EncodingAssertionTests
{
    [Test]
    public async Task Test_Encoding_IsUTF8()
    {
        var encoding = Encoding.UTF8;
        await Assert.That(encoding).IsUTF8();
    }

    [Test]
    public async Task Test_Encoding_IsNotUTF8()
    {
        var encoding = Encoding.ASCII;
        await Assert.That(encoding).IsNotUTF8();
    }

    [Test]
    public async Task Test_Encoding_IsASCII()
    {
        var encoding = Encoding.ASCII;
        await Assert.That(encoding).IsASCII();
    }

    [Test]
    public async Task Test_Encoding_IsUnicode()
    {
        var encoding = Encoding.Unicode;
        await Assert.That(encoding).IsUnicode();
    }

    [Test]
    public async Task Test_Encoding_IsUTF32()
    {
        var encoding = Encoding.UTF32;
        await Assert.That(encoding).IsUTF32();
    }

    [Test]
    public async Task Test_Encoding_IsBigEndianUnicode()
    {
        var encoding = Encoding.BigEndianUnicode;
        await Assert.That(encoding).IsBigEndianUnicode();
    }

    [Test]
    public async Task Test_Encoding_IsSingleByte()
    {
        var encoding = Encoding.ASCII;
        await Assert.That(encoding).IsSingleByte();
    }

    [Test]
    public async Task Test_Encoding_IsNotSingleByte()
    {
        var encoding = Encoding.UTF8;
        await Assert.That(encoding).IsNotSingleByte();
    }
}