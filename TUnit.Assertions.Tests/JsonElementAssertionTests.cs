using System.Text.Json;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class JsonElementAssertionTests
{
    [Test]
    public async Task IsObject_WithObject_Passes()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"test\"}");
        await Assert.That(doc.RootElement).IsObject();
    }

    [Test]
    public async Task IsObject_WithArray_Fails()
    {
        using var doc = JsonDocument.Parse("[1,2,3]");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).IsObject());
    }

    [Test]
    public async Task IsArray_WithArray_Passes()
    {
        using var doc = JsonDocument.Parse("[1,2,3]");
        await Assert.That(doc.RootElement).IsArray();
    }

    [Test]
    public async Task IsArray_WithNonArray_Fails()
    {
        using var doc = JsonDocument.Parse("{\"key\":\"value\"}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).IsArray());
    }

    [Test]
    public async Task IsString_WithString_Passes()
    {
        using var doc = JsonDocument.Parse("\"hello\"");
        await Assert.That(doc.RootElement).IsString();
    }

    [Test]
    public async Task IsString_WithNonString_Fails()
    {
        using var doc = JsonDocument.Parse("42");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).IsString());
    }

    [Test]
    public async Task IsNumber_WithNumber_Passes()
    {
        using var doc = JsonDocument.Parse("42");
        await Assert.That(doc.RootElement).IsNumber();
    }

    [Test]
    public async Task IsNumber_WithNonNumber_Fails()
    {
        using var doc = JsonDocument.Parse("\"text\"");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).IsNumber());
    }

    [Test]
    public async Task IsBoolean_WithTrue_Passes()
    {
        using var doc = JsonDocument.Parse("true");
        await Assert.That(doc.RootElement).IsBoolean();
    }

    [Test]
    public async Task IsBoolean_WithNonBoolean_Fails()
    {
        using var doc = JsonDocument.Parse("null");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).IsBoolean());
    }

    [Test]
    public async Task IsNull_WithNull_Passes()
    {
        using var doc = JsonDocument.Parse("null");
        await Assert.That(doc.RootElement).IsNull();
    }

    [Test]
    public async Task IsNull_WithNonNull_Fails()
    {
        using var doc = JsonDocument.Parse("{}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).IsNull());
    }

    [Test]
    public async Task IsNotNull_WithObject_Passes()
    {
        using var doc = JsonDocument.Parse("{}");
        await Assert.That(doc.RootElement).IsNotNull();
    }

    [Test]
    public async Task IsNotNull_WithNull_Fails()
    {
        using var doc = JsonDocument.Parse("null");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).IsNotNull());
    }

    [Test]
    public async Task HasProperty_WhenPropertyExists_Passes()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"Alice\",\"age\":30}");
        await Assert.That(doc.RootElement).HasProperty("name");
    }

    [Test]
    public async Task HasProperty_WhenPropertyMissing_Fails()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"Alice\"}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).HasProperty("missing"));
    }

    [Test]
    public async Task DoesNotHaveProperty_WhenPropertyMissing_Passes()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"Alice\"}");
        await Assert.That(doc.RootElement).DoesNotHaveProperty("missing");
    }

    [Test]
    public async Task DoesNotHaveProperty_WhenPropertyExists_Fails()
    {
        using var doc = JsonDocument.Parse("{\"name\":\"Alice\"}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc.RootElement).DoesNotHaveProperty("name"));
    }
}
