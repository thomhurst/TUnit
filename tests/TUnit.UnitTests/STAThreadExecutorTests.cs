using TUnit.Assertions.Extensions;
using TUnit.Core.Enums;
using TUnit.Core.Exceptions;

namespace TUnit.UnitTests;

public class STAThreadExecutorTests
{
    [Test]
    [ExcludeOn(OS.Windows)]
    public async Task Off_Windows_Throws_A_TUnitException_Naming_The_Remedy()
    {
#pragma warning disable CA1416 // Deliberately exercising the Windows-only executor off Windows.
        var executor = new STAThreadExecutor();

        var exception = await Assert.That(() => executor.ExecuteBeforeTestDiscoveryHook(null!, null!, () => default).AsTask())
            .Throws<TUnitException>();
#pragma warning restore CA1416

        await Assert.That(exception!.Message).Contains(nameof(STAThreadExecutor));
        await Assert.That(exception.Message).Contains("[RunOn(OS.Windows)]");
    }
}
