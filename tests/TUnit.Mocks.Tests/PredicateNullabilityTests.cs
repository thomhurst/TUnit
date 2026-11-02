using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

public record PredicateUser(string Name);

public interface IPredicateUserRepo
{
    string Save(PredicateUser user);
}

public interface INullablePredicateUserRepo
{
    string Save(PredicateUser? user);
}

/// <summary>
/// Regression tests for #7304: <c>Is&lt;T&gt;(predicate)</c> passes <c>T</c> (not <c>T?</c>) for a
/// non-nullable reference type, so predicates can dereference their argument without a null guard.
/// </summary>
public class PredicateNullabilityTests
{
    [Test]
    public async Task Predicate_For_NonNullable_Reference_Type_Dereferences_Without_Null_Guard()
    {
        var mock = IPredicateUserRepo.Mock();
        mock.Save(Is<PredicateUser>(u => u.Name == "Alice")).Returns("alice");

        IPredicateUserRepo repo = mock.Object;

        await Assert.That(repo.Save(new PredicateUser("Alice"))).IsEqualTo("alice");
        await Assert.That(repo.Save(new PredicateUser("Bob"))).IsNotEqualTo("alice");
    }

    [Test]
    public async Task Null_Argument_Does_Not_Match_Dereferencing_Predicate()
    {
        var mock = IPredicateUserRepo.Mock();
        mock.Save(Is<PredicateUser>(u => u.Name == "Alice")).Returns("alice");

        IPredicateUserRepo repo = mock.Object;

        await Assert.That(repo.Save(null!)).IsNotEqualTo("alice");
        mock.Save(Is<PredicateUser>(u => u.Name == "Alice")).WasNeverCalled();
    }

    [Test]
    public async Task Nullable_Predicate_Can_Match_Null()
    {
        var mock = INullablePredicateUserRepo.Mock();
        mock.Save(Is<PredicateUser?>(u => u is null)).Returns("null user");

        INullablePredicateUserRepo repo = mock.Object;

        await Assert.That(repo.Save(null)).IsEqualTo("null user");
        await Assert.That(repo.Save(new PredicateUser("Alice"))).IsNotEqualTo("null user");
    }

    [Test]
    public async Task Predicate_Exception_For_NonNull_Argument_Still_Propagates()
    {
        var mock = IPredicateUserRepo.Mock();
        mock.Save(Is<PredicateUser>(u => throw new NullReferenceException("from predicate"))).Returns("never");

        IPredicateUserRepo repo = mock.Object;

        await Assert.That(() => repo.Save(new PredicateUser("Alice"))).Throws<NullReferenceException>();
    }
}
