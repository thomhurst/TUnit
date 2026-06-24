using Issue6298Namespace;
using TUnit.Mocks;

// Regression for issue #6298: a fully-qualified call (Namespace.IFoo.Mock()) must generate the
// mock. The qualified form parses as member-access, not a simple name, which the discovery
// predicate previously dropped — causing CS1061. The interface is referenced only by qualified
// name at the call site, so this won't compile unless generation triggers off that syntax form.
namespace Issue6298Namespace
{
    public interface IIssue6298Interface
    {
        string Method();
    }
}

namespace TUnit.Mocks.Tests
{
    public class Issue6298Tests
    {
        [Test]
        public async Task Qualified_Name_Reference_Generates_Mock()
        {
            var mock = Issue6298Namespace.IIssue6298Interface.Mock();
            mock.Method().Returns("value");

            await Assert.That(mock.Object.Method()).IsEqualTo("value");
        }
    }
}
