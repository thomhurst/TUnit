namespace TUnit.Assertions.Tests.AssertionBuilders;

public sealed class OrAssertionTests
{
    [Test]
    public async Task Throws_When_Mixed_With_And()
    {
        var sut = "ABCD".ToCharArray();
#pragma warning disable TUnitAssertions0001
        var action = async () => await Assert.That(sut)
            .Contains('A').And
            .Contains('B').And
            .Contains('C').Or
            .Contains('D');
#pragma warning restore TUnitAssertions0001

        await Assert.That(action).Throws<MixedAndOrAssertionsException>();
    }

    [Test]
    public async Task Does_Not_Throw_For_Multiple_Or()
    {
        var sut = "ABCD".ToCharArray();
#pragma warning disable TUnitAssertions0001
        var action = async () => await Assert.That(sut)
            .Contains('A').Or
            .Contains('B').Or
            .Contains('C').Or
            .Contains('D');
#pragma warning restore TUnitAssertions0001

        await Assert.That(action).ThrowsNothing();
    }

    [Test]
    public async Task First_Assertion_Passes()
    {
        var value = 5;

        await Assert.That(value)
            .IsGreaterThan(3)
            .Or
            .IsLessThan(1);
    }

    [Test]
    public async Task Second_Assertion_Passes_When_First_Fails()
    {
        var value = 5;

        await Assert.That(value)
            .IsLessThan(3)
            .Or
            .IsGreaterThan(4);
    }

    [Test]
    public async Task Second_Assertion_Not_Evaluated_When_First_Passes()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;

        var value = 5;

        await Assert.That(value)
            .Satisfies(_ =>
            {
                firstEvaluated = true;
                return _ > 3; // This will pass
            })
            .Or
            .Satisfies(_ =>
            {
                secondEvaluated = true;
                return _ < 1;
            });

        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsFalse();
    }

    [Test]
    public async Task Second_Assertion_Is_Evaluated_When_First_Fails()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;

        var value = 5;

        await Assert.That(value)
            .Satisfies(_ =>
            {
                firstEvaluated = true;
                return _ > 10; // This will fail
            })
            .Or
            .Satisfies(_ =>
            {
                secondEvaluated = true;
                return _ < 10; // This will pass
            });

        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsTrue();
    }

    [Test]
    public async Task Error_Message_Shows_Combined_Expectations_When_Both_Fail()
    {
        var value = 5;

        var action = async () => await Assert.That(value)
            .IsGreaterThan(10)
            .Or
            .IsLessThan(3);

        var exception = await Assert.That(action).Throws<AssertionException>();

        await Assert.That(exception.Message)
            .Contains("to be greater than 10")
            .And
            .Contains("to be less than 3");
    }

    [Test]
    public async Task Three_Way_Or_Chain_First_Passes()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;
        var thirdEvaluated = false;

        var value = 5;

        await Assert.That(value)
            .Satisfies(_ =>
            {
                firstEvaluated = true;
                return _ > 3; // Passes
            })
            .Or
            .Satisfies(_ =>
            {
                secondEvaluated = true;
                return _ < 1;
            })
            .Or
            .Satisfies(_ =>
            {
                thirdEvaluated = true;
                return _ == 100;
            });

        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsFalse();
        await Assert.That(thirdEvaluated).IsFalse();
    }

    [Test]
    public async Task Three_Way_Or_Chain_Second_Passes()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;
        var thirdEvaluated = false;

        var value = 5;

        await Assert.That(value)
            .Satisfies(_ =>
            {
                firstEvaluated = true;
                return _ > 10; // Fails
            })
            .Or
            .Satisfies(_ =>
            {
                secondEvaluated = true;
                return _ < 10; // Passes
            })
            .Or
            .Satisfies(_ =>
            {
                thirdEvaluated = true;
                return _ == 100;
            });

        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsTrue();
        await Assert.That(thirdEvaluated).IsFalse();
    }

    [Test]
    public async Task Three_Way_Or_Chain_Third_Passes()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;
        var thirdEvaluated = false;

        var value = 5;

        await Assert.That(value)
            .Satisfies(_ =>
            {
                firstEvaluated = true;
                return _ > 10; // Fails
            })
            .Or
            .Satisfies(_ =>
            {
                secondEvaluated = true;
                return _ < 1; // Fails
            })
            .Or
            .Satisfies(_ =>
            {
                thirdEvaluated = true;
                return _ == 5; // Passes
            });

        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsTrue();
        await Assert.That(thirdEvaluated).IsTrue();
    }

    [Test]
    public async Task Three_Way_Or_Chain_All_Fail()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;
        var thirdEvaluated = false;

        var value = 5;

        var action = async () => await Assert.That(value)
            .Satisfies(_ =>
            {
                firstEvaluated = true;
                return _ > 10; // Fails
            })
            .Or
            .Satisfies(_ =>
            {
                secondEvaluated = true;
                return _ < 1; // Fails
            })
            .Or
            .Satisfies(_ =>
            {
                thirdEvaluated = true;
                return _ == 100; // Fails
            });

        await Assert.That(action).Throws<AssertionException>();
        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsTrue();
        await Assert.That(thirdEvaluated).IsTrue();
    }

    [Test]
    public async Task Within_Assert_Multiple_Second_Evaluated_When_First_Fails()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;

        using (Assert.Multiple())
        {
            await Assert.That(5)
                .Satisfies(_ =>
                {
                    firstEvaluated = true;
                    return _ > 10; // Fails
                })
                .Or
                .Satisfies(_ =>
                {
                    secondEvaluated = true;
                    return _ < 10; // Passes
                });
        }

        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsTrue();
    }

    [Test]
    public async Task Within_Assert_Multiple_Second_Not_Evaluated_When_First_Passes()
    {
        var firstEvaluated = false;
        var secondEvaluated = false;

        using (Assert.Multiple())
        {
            await Assert.That(5)
                .Satisfies(_ =>
                {
                    firstEvaluated = true;
                    return _ > 3; // Passes
                })
                .Or
                .Satisfies(_ =>
                {
                    secondEvaluated = true;
                    return _ < 1;
                });
        }

        await Assert.That(firstEvaluated).IsTrue();
        await Assert.That(secondEvaluated).IsFalse();
    }

    [Test]
    public async Task Within_Assert_Multiple_Combined_Error_Message_When_Both_Fail()
    {
        var action = async () =>
        {
            using (Assert.Multiple())
            {
                await Assert.That(5)
                    .IsGreaterThan(10)
                    .Or
                    .IsLessThan(3);
            }
        };

        var exception = await Assert.That(action).Throws<Exception>();

        await Assert.That(exception.Message)
            .Contains("to be greater than 10")
            .And
            .Contains("to be less than 3");
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
                return _ > 10; // Fails
            })
            .Or
            .Satisfies(_ =>
            {
                evaluationOrder.Add(2);
                return _ < 10; // Passes
            });

        await Assert.That(evaluationOrder).HasCount(2);
        await Assert.That(evaluationOrder[0]).IsEqualTo(1);
        await Assert.That(evaluationOrder[1]).IsEqualTo(2);
    }

    [Test]
    public async Task Short_Circuits_Verified_With_Counter()
    {
        var evaluationOrder = new List<int>();

        var value = 5;

        await Assert.That(value)
            .Satisfies(_ =>
            {
                evaluationOrder.Add(1);
                return _ > 3; // Passes
            })
            .Or
            .Satisfies(_ =>
            {
                evaluationOrder.Add(2);
                return _ < 10;
            });

        await Assert.That(evaluationOrder).HasCount(1);
        await Assert.That(evaluationOrder[0]).IsEqualTo(1);
    }

    [Test]
    public async Task Or_With_Collection_Assertions()
    {
        var collection = new[] { 1, 2, 3 };

        await Assert.That(collection)
            .Contains(5)
            .Or
            .HasCount(3);
    }

    [Test]
    public async Task Or_With_String_Assertions()
    {
        var text = "Hello World";

        await Assert.That(text)
            .Contains("Goodbye")
            .Or
            .HasLength(11);
    }

    // [Test]
    // [Skip("Extension method resolution issues with Polyfill package")]
    // public async Task Short_Circuits_When_First_Assertion_Succeeds()
    // {
    //     var exception = await Assert.That(() => throw new InvalidOperationException()).ThrowsException();
    //     await Assert.That(exception)
    //                 .IsNotAssignableTo<ArgumentOutOfRangeException>()
    //                 .Or
    //                 .Satisfies(x => (ArgumentOutOfRangeException) x,
    //                            x => x.HasMember(y => y!.ActualValue).EqualTo("foo"));
    // }
}
