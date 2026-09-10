namespace TUnit.PerformanceBenchmarks.Tests.ConstraintKeys;

/// <summary>
/// Stress tests for ConstraintKeyScheduler with overlapping constraint keys.
/// These tests create high contention scenarios to exercise lock contention paths.
/// </summary>
public class ConstraintKeyStressTests
{
    // Single constraint key tests - 50 instances each, must run serially within key group
    [Test, Repeat(50), NotInParallel("KeyA")]
    public async Task Test_SingleKey_A() => await Task.Delay(1);

    [Test, Repeat(50), NotInParallel("KeyB")]
    public async Task Test_SingleKey_B() => await Task.Delay(1);

    [Test, Repeat(50), NotInParallel("KeyC")]
    public async Task Test_SingleKey_C() => await Task.Delay(1);

    [Test, Repeat(50), NotInParallel("KeyD")]
    public async Task Test_SingleKey_D() => await Task.Delay(1);

    [Test, Repeat(50), NotInParallel("KeyE")]
    public async Task Test_SingleKey_E() => await Task.Delay(1);

    // Overlapping two-key tests - creates dependency chains between key groups
    [Test, Repeat(30), NotInParallel(["KeyA", "KeyB"])]
    public async Task Test_OverlappingKeys_AB() => await Task.Delay(1);

    [Test, Repeat(30), NotInParallel(["KeyB", "KeyC"])]
    public async Task Test_OverlappingKeys_BC() => await Task.Delay(1);

    [Test, Repeat(30), NotInParallel(["KeyC", "KeyD"])]
    public async Task Test_OverlappingKeys_CD() => await Task.Delay(1);

    [Test, Repeat(30), NotInParallel(["KeyD", "KeyE"])]
    public async Task Test_OverlappingKeys_DE() => await Task.Delay(1);

    [Test, Repeat(30), NotInParallel(["KeyE", "KeyA"])]
    public async Task Test_OverlappingKeys_EA() => await Task.Delay(1);

    // Triple-key tests - maximum contention scenarios
    [Test, Repeat(20), NotInParallel(["KeyA", "KeyB", "KeyC"])]
    public async Task Test_TripleKeys_ABC() => await Task.Delay(1);

    [Test, Repeat(20), NotInParallel(["KeyC", "KeyD", "KeyE"])]
    public async Task Test_TripleKeys_CDE() => await Task.Delay(1);
}
