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
            .Satisfies(model => model.Value, assert => assert.IsEqualTo("Hello")!);
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
            .Satisfies(model => model.Nested, assert =>
                assert.Satisfies(model => model?.Nested, innerAssert =>
                    innerAssert.Satisfies(model => model?.Value, innerAssert2 =>
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
                    .Satisfies(model => model.Value, assert => assert.IsEqualTo("Blah")!)
            ).Throws<AssertionException>()
            .WithMessageMatching("""
                                 *Expected model => model.Value to satisfy assert => assert.IsEqualTo("Blah")!
                                 
                                 but found "Hello" which differs at index 0:*
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
                *Expected model => model.Nested to satisfy assert =>
                                        assert.Satisfies(model => model?.Nested, innerAssert =>
                                            innerAssert.Satisfies(model => model?.Value, innerAssert2 =>
                                                innerAssert2.IsEqualTo("Blah")!
                                            )
                                        )
                
                but found "Baz" which differs at index 1:
                     ↓
                   "Baz"
                   "Blah"
                     ↑*
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
                    .Satisfies(model => model.Value, assert => assert.IsEqualTo("Blah")!)
            ).Throws<AssertionException>()
            .WithMessageMatching("""
                                 *Expected model => model.Value to satisfy assert => assert.IsEqualTo("Blah")!
                                 
                                 but found "Hello" which differs at index 0:*
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
                    .Satisfies(model => model.Nested, assert =>
                        assert.Satisfies(model => model?.Nested, innerAssert =>
                            innerAssert.Satisfies(model => model?.Value, innerAssert2 =>
                                innerAssert2.IsEqualTo("Baz")!
                            )
                        )
                    )
            ).Throws<AssertionException>()
            .WithMessageMatching(
                """
                *Expected model => model.Nested to satisfy assert =>
                                        assert.Satisfies(model => model?.Nested, innerAssert =>
                                            innerAssert.Satisfies(model => model?.Value, innerAssert2 =>
                                                innerAssert2.IsEqualTo("Baz")!
                                            )
                                        )
                
                but found "Blah" which differs at index 1:
                     ↓
                   "Blah"
                   "Baz"
                     ↑
                
                at Assert.That(myModel).Satisfies(model => model.Nested, assert =>
                                        assert.Sati...*
                """
                );
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