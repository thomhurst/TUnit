using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Executors;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[WindowsOnly]
[Repeat(100)]
[UnconditionalSuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class STAThreadTests
{
    [Test, STAThreadExecutor]
    public async Task With_STA()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test]
    public async Task Without_STA()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.MTA);
    }
}
