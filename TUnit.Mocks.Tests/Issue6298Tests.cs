using Issue6298Namespace;
using TUnit.Mocks;

// Regression for issue #6298: invoking .Mock() through a fully-qualified type reference
// (Namespace.IFoo.Mock()) must still generate the mock surface. The qualified reference parses
// as a member-access expression rather than a simple identifier, which the discovery predicate
// previously dropped — so generation never triggered and usage failed to compile with CS1061.
// The interface is referenced exclusively via its qualified name at the call site (mirroring the
// issue's repro), so the test would not compile unless generation triggers off that syntax form.
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
