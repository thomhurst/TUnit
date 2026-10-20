using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7173;

/// <summary>
/// Only a parameter declared as CancellationToken receives the test's token. A type parameter closed
/// over CancellationToken is a data parameter and takes the row value (#7173).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class ClosedGenericCancellationTokenDataTests
{
    public static IEnumerable<CancellationToken> CancelledTokenRows()
    {
        yield return new CancellationToken(canceled: true);
    }

    [Test]
    [GenerateGenericTest(typeof(CancellationToken))]
    [MethodDataSource(nameof(CancelledTokenRows))]
    public async Task Generic_Closed_Over_Token_Takes_Row_Value<T>(T value, CancellationToken token)
    {
        await Assert.That(((CancellationToken)(object)value!).IsCancellationRequested).IsTrue();
        await Assert.That(token.IsCancellationRequested).IsFalse();
    }
}
