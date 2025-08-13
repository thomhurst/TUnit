using TUnit.Core;

namespace TUnit.TestProject
{
    public class ParamsArgumentsTests
    {
        [Test]
        [Arguments(2, 2)]
        [Arguments(20, 3, Operation.Kind.A)]
        [Arguments(20, 6, Operation.Kind.Deposit, Operation.Kind.B)]
        public void GetOperations(int dayDelta, int expectedNumberOfOperation, params Operation.Kind[] kinds)
        {
            // Test implementation
        }
    }

    public class Operation
    {
        public enum Kind
        {
            A,
            B,
            Deposit
        }
    }
}