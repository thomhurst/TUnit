namespace TUnit.Assertions.Tests;

public class SatisfiesTests
{
    [Test]
    public async Task Satisfies_On_Direct_Property()
    {
        var myModel = new MyModel
        {
            Value = "Hello"
        };

        await Assert.That(myModel)
            .Satisfies(model => model.Value, assert => assert.IsEqualTo("Hello")!);
    }

    [Test]
    public async Task Satisfies_On_Nested_Property()
    {
        var myModel = new MyModel
        {
            Value = "Foo",
            Nested = new MyModel
            {
                Value = "Bar",
                Nested = new MyModel
                {
                    Value = "Baz"
                }
            }
        };

        await Assert.That(myModel)
            .Satisfies(model => model.Nested, assert =>
                assert.Satisfies(model => model?.Nested, innerAssert =>
                    innerAssert.Satisfies(model => model?.Value, innerAssert2 =>
                        innerAssert2.IsEqualTo("Baz")!
                    )
                )
            );
    }

    [Test]
    public async Task Async_Satisfies_On_Direct_Property()
    {
        var myModel = new MyAsyncModel
        {
            Value = Task.FromResult("Hello")!
        };

        await Assert.That(myModel)
            .SatisfiesAsync(async model => await model.Value!, assert => assert.IsEqualTo("Hello")!);
    }

    [Test]
    public async Task Async_Satisfies_On_Nested_Property()
    {
        var myModel = new MyAsyncModel
        {
            Value = Task.FromResult("Foo")!,
            Nested = Task.FromResult(new MyAsyncModel
            {
                Value = Task.FromResult("Bar")!,
                Nested = Task.FromResult(new MyAsyncModel
                {
                    Value = Task.FromResult("Baz")!
                })!
            })!
        };

        await Assert.That(myModel)
            .SatisfiesAsync(async model => await model.Nested!, assert =>
                assert.SatisfiesAsync(async model => await model?.Nested!, innerAssert =>
                    innerAssert.SatisfiesAsync(async model => await model?.Value!, innerAssert2 =>
                        innerAssert2.IsEqualTo("Baz")!
                    )
                )
            );
    }

    [Test]
    public async Task Satisfies_On_Direct_Property_Throws()
    {
        var myModel = new MyModel
        {
            Value = "Hello"
        };

        await Assert.That(async () =>
                await Assert.That(myModel)
                    .Satisfies(model => model.Value!, assert => assert.IsEqualTo("Blah")!)
            ).Throws<AssertionException>()
            .WithMessageMatching("""
                                 *to satisfy*
                                 *found "Hello"*
                                 """);
    }

    [Test]
    public async Task Satisfies_On_Nested_Property_Throws()
    {
        var myModel = new MyModel
        {
            Value = "Foo",
            Nested = new MyModel
            {
                Value = "Bar",
                Nested = new MyModel
                {
                    Value = "Baz"
                }
            }
        };

        await Assert.That(async () =>
                await Assert.That(myModel)
                    .Satisfies(model => model.Nested, assert =>
                        assert.Satisfies(model => model?.Nested, innerAssert =>
                            innerAssert.Satisfies(model => model?.Value, innerAssert2 =>
                                innerAssert2.IsEqualTo("Blah")!
                            )
                        )
                    )
            ).Throws<AssertionException>()
            .WithMessageMatching(
                """
                *to satisfy*
                *found "Baz"*
                """
                );
    }

    [Test]
    public async Task Async_Satisfies_On_Direct_Property_Throws()
    {
        var myModel = new MyAsyncModel
        {
            Value = Task.FromResult("Hello")!
        };

        await Assert.That(async () =>
                await Assert.That(myModel)
                    .SatisfiesAsync(async model => await model.Value!, assert => assert.IsEqualTo("Blah")!)
            ).Throws<AssertionException>()
            .WithMessageMatching("""
                                 *to satisfy*
                                 *found "Hello"*
                                 """);
    }

    [Test]
    public async Task Async_Satisfies_On_Nested_Property_Throws()
    {
        var myModel = new MyAsyncModel
        {
            Value = Task.FromResult("Foo")!,
            Nested = Task.FromResult(new MyAsyncModel
            {
                Value = Task.FromResult("Bar")!,
                Nested = Task.FromResult(new MyAsyncModel
                {
                    Value = Task.FromResult("Blah")!
                })!
            })!
        };

        await Assert.That(async () =>
                await Assert.That(myModel)
                    .SatisfiesAsync(async model => await model.Nested!, assert =>
                        assert.SatisfiesAsync(async model => await model?.Nested!, innerAssert =>
                            innerAssert.SatisfiesAsync(async model => await model?.Value!, innerAssert2 =>
                                innerAssert2.IsEqualTo("Baz")!
                            )
                        )
                    )
            ).Throws<AssertionException>()
            .WithMessageMatching(
                """
                *found "Blah"*
                """
                );
    }

    [Test]
    public async Task All_Satisfy_Mapper_Good()
    {
        var myModel = new MyModel { Value = "Hello" };
        var myModel2 = new MyModel { Value = "World" };
        var myModel3 = new MyModel { Value = "!" };
        List<MyModel> models = [myModel, myModel2, myModel3];

        await Assert.That(models).All().Satisfy(model => model?.Value?.Length ?? 0, assert => assert.IsGreaterThan(0));
    }

    [Test]
    public async Task All_Satisfy_DirectValue_Good()
    {
        var myModel = new MyModel { Value = "Hello" };
        var myModel2 = new MyModel { Value = "World" };
        var myModel3 = new MyModel { Value = "!" };
        List<MyModel> models = [myModel, myModel2, myModel3];

        await Assert.That(models).All().Satisfy(assert => assert.IsNotNull());
    }

    [Test]
    public async Task All_Satisfy_DirectValue_Throws()
    {
        var myModel = new MyModel { Value = "Hello" };
        var myModel2 = new MyModel { Value = "World" };
        var myModel3 = new MyModel { Value = "!" };
        List<MyModel?> models = [myModel, myModel2, myModel3, null];

        await Assert.That(async () =>
            await Assert.That(models).All().Satisfy(assert => assert.IsNotNull())
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task All_Satisfy_Mapped_Throws()
    {
        var myModel = new MyModel { Value = "Hello" };
        var myModel2 = new MyModel { Value = "Wrld" };
        var myModel3 = new MyModel { Value = "!" };
        List<MyModel?> models = [myModel, myModel2, myModel3];

        await Assert.That(async () =>
                await Assert.That(models).All().Satisfy(model => model!.Value, item => item.Contains("o")!)
        ).Throws<AssertionException>().WithMessageMatching("""
                                                           *to satisfy*
                                                           *index 1*
                                                           """);
    }

    // Tests for issue #3766: Demonstrates the correct usage of .All().Satisfy()
    // The mapper overload allows checking properties of collection items
    [Test]
    public async Task All_Satisfy_WithMapper_Good()
    {
        var users = new[]
        {
            new User { Name = "Alice", Age = 25 },
            new User { Name = "Bob", Age = 30 }
        };

        await Assert.That(users)
            .All()
            .Satisfy(u => u.Age, age => age.IsGreaterThan(18));
    }

    [Test]
    public async Task All_Satisfy_WithMapper_Throws()
    {
        var users = new[]
        {
            new User { Name = "Alice", Age = 25 },
            new User { Name = "Bob", Age = 15 }
        };

        await Assert.That(async () =>
            await Assert.That(users)
                .All()
                .Satisfy(u => u.Age, age => age.IsGreaterThan(18))
        ).Throws<AssertionException>().WithMessageMatching("""
                                                           *to satisfy*
                                                           *index 1*
                                                           """);
    }

    [Test]
    public async Task Satisfies_With_Member_As_Final_Statement()
    {
        var list = new List<MyModelWithId>
        {
            new() { Id = 1, Name = "First" }
        };

        await Assert.That(list).Satisfies(
            l => l.First(),
            item => item.Member(i => i.Id, v => v.EqualTo(1)));
    }

    [Test]
    public async Task Satisfies_With_Member_After_Chaining()
    {
        var list = new List<MyModelWithId>
        {
            new() { Id = 1, Name = "First" }
        };

        await Assert.That(list).Satisfies(
            l => l.First(),
            item => item.IsNotNull()
                .And.Member(i => i.Id, v => v.EqualTo(1)));
    }

    [Test]
    public async Task Satisfies_With_Member_Before_Other_Assertions()
    {
        var list = new List<MyModelWithId>
        {
            new() { Id = 1, Name = "First" }
        };

        await Assert.That(list).Satisfies(
            l => l.First(),
            item => item.Member(i => i.Id, v => v.EqualTo(1))
                .And.IsNotNull());
    }

    [Test]
    public async Task Satisfies_With_Member_Fails_Correctly()
    {
        var list = new List<MyModelWithId>
        {
            new() { Id = 2, Name = "First" }
        };

        await Assert.That(async () =>
            await Assert.That(list).Satisfies(
                l => l.First(),
                item => item.IsNotNull()
                    .And.Member(i => i.Id, v => v.EqualTo(1)))
        ).Throws<AssertionException>().WithMessageMatching("""
                                                           *to satisfy*
                                                           """);
    }

    public class User
    {
        public string Name { get; init; } = string.Empty;
        public int Age { get; init; }
    }

    public class MyModelWithId
    {
        public int Id { get; init; }
        public string? Name { get; init; }
    }

    public class MyModel
    {
        public string? Value { get; init; }
        public MyModel? Nested { get; init; }
    }

    public class MyAsyncModel
    {
        public Task<string?>? Value { get; init; }
        public Task<MyAsyncModel?>? Nested { get; init; }
    }
}
