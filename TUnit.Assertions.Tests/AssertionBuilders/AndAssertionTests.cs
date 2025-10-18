namespace TUnit.Assertions.Tests.AssertionBuilders;

public sealed class AndAssertionTests
{
    [Test]
    public async Task Throws_When_Mixed_With_Or()
    {
        var sut = "ABCD".ToCharArray();
#pragma warning disable TUnitAssertions0001
        var action = async () => await Assert.That(sut)
            .Contains('A').Or
            .Contains('B').Or
            .Contains('C').And
            .Contains('D');
#pragma warning restore TUnitAssertions0001

        await Assert.That(action).Throws<MixedAndOrAssertionsException>();
    }

    [Test]
    public async Task Does_Not_Throw_For_Multiple_And()
    {
        var sut = "ABCD".ToCharArray();
#pragma warning disable TUnitAssertions0001
        var action = async () => await Assert.That(sut)
            .Contains('A').And
            .Contains('B').And
            .Contains('C').And
            .Contains('D');
#pragma warning restore TUnitAssertions0001

        await Assert.That(action).ThrowsNothing();
    }

    [Test]
    public async Task Both_Assertions_Pass()
    {
        var value = 5;

        await Assert.That(value)
            .IsGreaterThan(3)
            .And
            .IsLessThan(10);
    }

    [Test]
    public async Task Both_Assertions_Are_Evaluated_When_First_Passes()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;

        var value = 5;

        await Assert.That(value)
            .Satisfies(_ =>
            {
                firstEvaluated = true;
                return _ > 3;
            })
            .And
            .Satisfies(_ =>
            {
                secondEvaluated = true;
                return _ < 10;
            });

        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsTrue();
    }

    [Test]
    public async Task Second_Assertion_Not_Evaluated_When_First_Fails()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;

        var value = 5;

        var action = async () => await Assert.That(value)
            .Satisfies(_ =>
            {
                firstEvaluated = true;
                return _ > 10; // This will fail
            })
            .And
            .Satisfies(_ =>
            {
                secondEvaluated = true;
                return _ < 20;
            });

        await Assert.That(action).Throws<AssertionException>();
        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsFalse();
    }

    [Test]
    public async Task Error_Message_Shows_Combined_Expectations_When_Second_Fails()
    {
        var value = 5;

        var action = async () => await Assert.That(value)
            .IsGreaterThan(3)
            .And
            .IsLessThan(4);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message)
            .Contains("to be greater than 3")
            .And
            .Contains("to be less than 4");
    }

    [Test]
    public async Task Error_Message_Shows_First_Expectation_When_First_Fails()
    {
        var value = 5;

        var action = async () => await Assert.That(value)
            .IsGreaterThan(10)
            .And
            .IsLessThan(20);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message)
            .Contains("to be greater than 10");
    }

    [Test]
    public async Task Three_Way_And_Chain_All_Pass()
    {
        var value = 5;

        await Assert.That(value)
            .IsGreaterThan(3)
            .And
            .IsLessThan(10)
            .And
            .IsEqualTo(5);
    }

    [Test]
    public async Task Three_Way_And_Chain_Middle_Fails()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;
        var thirdEvaluated = false;

        var value = 5;

        var action = async () => await Assert.That(value)
            .Satisfies(_ =>
            {
                firstEvaluated = true;
                return _ > 3; // Passes
            })
            .And
            .Satisfies(_ =>
            {
                secondEvaluated = true;
                return _ > 10; // Fails
            })
            .And
            .Satisfies(_ =>
            {
                thirdEvaluated = true;
                return _ < 20;
            });

        await Assert.That(action).Throws<AssertionException>();
        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsTrue();
        await Assert.That(thirdEvaluated).IsFalse();
    }

    [Test]
    public async Task Three_Way_And_Chain_Last_Fails()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;
        var thirdEvaluated = false;

        var value = 5;

        var action = async () => await Assert.That(value)
            .Satisfies(_ =>
            {
                firstEvaluated = true;
                return _ > 3; // Passes
            })
            .And
            .Satisfies(_ =>
            {
                secondEvaluated = true;
                return _ < 10; // Passes
            })
            .And
            .Satisfies(_ =>
            {
                thirdEvaluated = true;
                return _ > 20; // Fails
            });

        await Assert.That(action).Throws<AssertionException>();
        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsTrue();
        await Assert.That(thirdEvaluated).IsTrue();
    }

    [Test]
    public async Task Within_Assert_Multiple_Both_Assertions_Evaluated()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;

        var action = async () =>
        {
            using (Assert.Multiple())
            {
                await Assert.That(5)
                    .Satisfies(_ =>
                    {
                        firstEvaluated = true;
                        return _ > 10; // Fails
                    })
                    .And
                    .Satisfies(_ =>
                    {
                        secondEvaluated = true;
                        return _ < 20;
                    });
            }
        };

        await Assert.That(action).Throws<Exception>();
        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsFalse();
    }

    [Test]
    public async Task Within_Assert_Multiple_Combined_Error_Message_When_Second_Fails()
    {
        var action = async () =>
        {
            using (Assert.Multiple())
            {
                await Assert.That(5)
                    .IsGreaterThan(3)
                    .And
                    .IsLessThan(4);
            }
        };

        var exception = await Assert.That(action).Throws<Exception>();

        await Assert.That(exception.Message)
            .Contains("to be greater than 3")
            .And
            .Contains("to be less than 4");
    }

    [Test]
    public async Task Evaluation_Order_Verified_With_Counter()
    {
        var evaluationOrder = new List<int>();

        var value = 5;

        await Assert.That(value)
            .Satisfies(_ =>
            {
                evaluationOrder.Add(1);
                return _ > 3;
            })
            .And
            .Satisfies(_ =>
            {
                evaluationOrder.Add(2);
                return _ < 10;
            });

        await Assert.That(evaluationOrder).HasCount(2);
        await Assert.That(evaluationOrder[0]).IsEqualTo(1);
        await Assert.That(evaluationOrder[1]).IsEqualTo(2);
    }

    [Test]
    public async Task And_With_Collection_Assertions()
    {
        var collection = new[] { 1, 2, 3, 4, 5 };

        await Assert.That(collection)
            .Contains(3)
            .And
            .HasCount(5);
    }

    [Test]
    public async Task And_With_String_Assertions()
    {
        var text = "Hello World";

        await Assert.That(text)
            .Contains("Hello")
            .And
            .HasLength(11);
    }
}
