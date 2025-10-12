using System.Text;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class StringBuilderAssertionTests
{
    [Test]
    public async Task Test_StringBuilder_IsEmpty()
    {
        var sb = new StringBuilder();
        await Assert.That(sb).IsEmpty();
    }

    [Test]
    public async Task Test_StringBuilder_IsEmpty_WithCapacity()
    {
        var sb = new StringBuilder(100);
        await Assert.That(sb).IsEmpty();
    }

    [Test]
    public async Task Test_StringBuilder_IsNotEmpty()
    {
        var sb = new StringBuilder("Hello");
        await Assert.That(sb).IsNotEmpty();
    }

    [Test]
    public async Task Test_StringBuilder_IsNotEmpty_SingleChar()
    {
        var sb = new StringBuilder("A");
        await Assert.That(sb).IsNotEmpty();
    }

    [Test]
    public async Task Test_StringBuilder_HasExcessCapacity()
    {
        var sb = new StringBuilder(100);
        sb.Append("Hello");
        await Assert.That(sb).HasExcessCapacity();
    }

    [Test]
    public async Task Test_StringBuilder_HasExcessCapacity_LargeCapacity()
    {
        var sb = new StringBuilder(1000);
        await Assert.That(sb).HasExcessCapacity();
    }
}
