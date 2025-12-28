using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class JsonStringAssertionTests
{
    [Test]
    public async Task IsValidJson_WithValidJson_Passes()
    {
        var json = "{\"name\":\"Alice\"}";
        await Assert.That(json).IsValidJson();
    }

    [Test]
    public async Task IsValidJson_WithInvalidJson_Fails()
    {
        var json = "not valid json";
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(json).IsValidJson());
    }

    [Test]
    public async Task IsNotValidJson_WithInvalidJson_Passes()
    {
        var json = "not valid json";
        await Assert.That(json).IsNotValidJson();
    }

    [Test]
    public async Task IsNotValidJson_WithValidJson_Fails()
    {
        var json = "{\"name\":\"Alice\"}";
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(json).IsNotValidJson());
    }

    [Test]
    public async Task IsValidJsonObject_WithObject_Passes()
    {
        var json = "{\"name\":\"Alice\"}";
        await Assert.That(json).IsValidJsonObject();
    }

    [Test]
    public async Task IsValidJsonObject_WithArray_Fails()
    {
        var json = "[1,2,3]";
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(json).IsValidJsonObject());
    }

    [Test]
    public async Task IsValidJsonObject_WithInvalidJson_Fails()
    {
        var json = "not valid json";
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(json).IsValidJsonObject());
    }

    [Test]
    public async Task IsValidJsonArray_WithArray_Passes()
    {
        var json = "[1,2,3]";
        await Assert.That(json).IsValidJsonArray();
    }

    [Test]
    public async Task IsValidJsonArray_WithObject_Fails()
    {
        var json = "{\"name\":\"Alice\"}";
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(json).IsValidJsonArray());
    }

    [Test]
    public async Task IsValidJsonArray_WithInvalidJson_Fails()
    {
        var json = "not valid json";
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(json).IsValidJsonArray());
    }
}
