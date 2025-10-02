using System.Diagnostics;
using System.Transactions;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

#if NET
[EngineTest(ExpectedResult.Pass)]
public class TransactionTest
{
    private TransactionScope _scope;

    [Before(Test)]
    public void Setup(TestContext context)
    {
        _scope = new TransactionScope(
            TransactionScopeOption.RequiresNew,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        Debug.Assert(Transaction.Current is not null);

        context.AddAsyncLocalValues();
    }

    [Test]
    public async Task TransactionScopeIsSet()
    {
        await Assert.That(Transaction.Current)
            .IsNotNull(); // Always fails unless the TransactionScope is created in the test itself
    }
}
#endif
