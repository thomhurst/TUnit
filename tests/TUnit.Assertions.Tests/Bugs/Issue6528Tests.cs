namespace TUnit.Assertions.Tests.Bugs;

public class Issue6528Tests
{
    [Test]
    public async Task Nested_Member_After_IsTypeOf_Succeeds()
    {
        Action action = () => throw new OuterEx(new InnerEx(5));

        await Assert.That(action)
            .ThrowsExactly<OuterEx>()
            .And.Member(
                exception => exception.InnerException,
                assertion => assertion.IsTypeOf<InnerEx>()
                    .And.Member(inner => inner.Code, code => code.IsEqualTo(5)));
    }

    [Test]
    public async Task Nested_Member_After_IsTypeOf_Executes_Nested_Assertion()
    {
        Action action = () => throw new OuterEx(new InnerEx(4));

        await Assert.ThrowsAsync<AssertionException>(async () =>
        {
            await Assert.That(action)
                .ThrowsExactly<OuterEx>()
                .And.Member(
                    exception => exception.InnerException,
                    assertion => assertion.IsTypeOf<InnerEx>()
                        .And.Member(inner => inner.Code, code => code.IsEqualTo(5)));
        });
    }

    [Test]
    public async Task Nested_Member_After_IsTypeOf_Executes_Type_Assertion()
    {
        Action action = () => throw new OuterEx(new InvalidOperationException());

        await Assert.ThrowsAsync<AssertionException>(async () =>
        {
            await Assert.That(action)
                .ThrowsExactly<OuterEx>()
                .And.Member(
                    exception => exception.InnerException,
                    assertion => assertion.IsTypeOf<InnerEx>()
                        .And.Member(inner => inner.Code, code => code.IsEqualTo(5)));
        });
    }

    private sealed class OuterEx(Exception innerException) : Exception("Outer exception", innerException);

    private sealed class InnerEx(int code) : Exception
    {
        public int Code { get; } = code;
    }
}
