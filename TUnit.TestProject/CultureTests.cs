using System.Globalization;
using TUnit.Assertions;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Extensions;
using TUnit.Core.Executors;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class CultureTests
{
    [Test, Culture("en-GB")]
    public async Task Test1()
    {
        await Assert.That(CultureInfo.CurrentCulture.Name).IsEqualTo("en-GB");
        await Assert.That(double.Parse("3.5")).IsEqualTo(3.5);
    }

    [Test, InvariantCulture]
    public async Task Test2()
    {
        await Assert.That(CultureInfo.CurrentCulture).IsEqualTo(CultureInfo.InvariantCulture);
        await Assert.That(double.Parse("3.5")).IsEqualTo(3.5);
    }

    [Test, Culture("de-AT")]
    public async Task Test3()
    {
        await Assert.That(CultureInfo.CurrentCulture.Name).IsEqualTo("de-AT");
        await Assert.That(double.Parse("3,5")).IsEqualTo(3.5);
    }
}