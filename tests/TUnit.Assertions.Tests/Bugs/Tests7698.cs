using System.Collections;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/7698:
/// Assert.That on a null non-generic IEnumerable threw ArgumentNullException,
/// so IsNull() could never pass.
/// </summary>
public class Tests7698
{
    [Test]
    public async Task IsNull_Passes_For_Null_NonGeneric_IEnumerable()
    {
        IEnumerable? value = null;

        await Assert.That(value).IsNull();
    }

    [Test]
    public async Task IsNotNull_Fails_With_AssertionException_For_Null_NonGeneric_IEnumerable()
    {
        IEnumerable? value = null;

        await Assert.That(async () => await Assert.That(value).IsNotNull())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsEmpty_Fails_With_AssertionException_For_Null_NonGeneric_IEnumerable()
    {
        IEnumerable? value = null;

        await Assert.That(async () => await Assert.That(value).IsEmpty())
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Contains_Fails_With_AssertionException_For_Null_NonGeneric_IEnumerable()
    {
        IEnumerable? value = null;

        await Assert.That(async () => await Assert.That(value).Contains(1))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsNotNull_Passes_For_NonNull_NonGeneric_IEnumerable()
    {
        IEnumerable value = new ArrayList { 1, 2, 3 };

        await Assert.That(value).IsNotNull();
    }
}
