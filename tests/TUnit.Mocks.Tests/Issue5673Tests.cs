#if NET10_0_OR_GREATER
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/5673
// EntityEntry implements IInfrastructure<InternalEntityEntry> where InternalEntityEntry is
// internal to EF Core. The generated override for `Instance` referenced that internal type,
// producing CS0115 "no suitable method found to override" in external assemblies.
public class Issue5673Tests
{
    [Test]
    public async Task Mocking_EntityEntry_With_Internal_Return_Type_Compiles()
    {
        // If the generator regresses, this file fails to compile with CS0115 on 'Instance'.
        // EntityEntry only exposes a (InternalEntityEntry) ctor, so pass null through the
        // generated overload.
        var mock = EntityEntry.Mock(internalEntry: null!, MockBehavior.Loose);
        await Assert.That(mock).IsNotNull();
    }
}
#endif
