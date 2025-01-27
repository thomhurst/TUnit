using TUnit.Assertions.AssertConditions.Throws;

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

    [Test]
    public async Task All_Satisfy_Mapper_Good()
    {
        var myModel = new MyModel { Value = "Hello" };
        var myModel2 = new MyModel { Value = "World" };
        var myModel3 = new MyModel { Value = "!" };
        List<MyModel> models = [myModel, myModel2, myModel3];

        await Assert.That(models).All().Satisfy(model => model?.Value, assert => assert.HasCount().Positive());
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
                                                           *Expected items mapped by (MyModel? model) => model!.Value to satisfy item => item.Contains("o")!
                                                           
                                                           but items not satisfying the condition were found:
                                                           at [1] it was not found. Found a closest match which differs at index 0:
                                                               ↓
                                                              "Wrld"
                                                              "o"
                                                               ↑
                                                           at [2] it was not found. Found a closest match which differs at index 0:
                                                               ↓
                                                              "!"
                                                              "o"
                                                               ↑*
                                                           """);
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