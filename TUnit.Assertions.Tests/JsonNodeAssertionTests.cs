using System.Text.Json.Nodes;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class JsonNodeAssertionTests
{
    [Test]
    public async Task IsJsonObject_WithJsonObject_Passes()
    {
        JsonNode? node = JsonNode.Parse("{\"name\":\"test\"}");
        await Assert.That(node).IsJsonObject();
    }

    [Test]
    public async Task IsJsonObject_WithJsonArray_Fails()
    {
        JsonNode? node = JsonNode.Parse("[1,2,3]");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(node).IsJsonObject());
    }

    [Test]
    public async Task IsJsonArray_WithJsonArray_Passes()
    {
        JsonNode? node = JsonNode.Parse("[1,2,3]");
        await Assert.That(node).IsJsonArray();
    }

    [Test]
    public async Task IsJsonArray_WithJsonObject_Fails()
    {
        JsonNode? node = JsonNode.Parse("{\"name\":\"test\"}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(node).IsJsonArray());
    }

    [Test]
    public async Task IsJsonValue_WithJsonValue_Passes()
    {
        JsonNode? node = JsonNode.Parse("42");
        await Assert.That(node).IsJsonValue();
    }

    [Test]
    public async Task IsJsonValue_WithJsonObject_Fails()
    {
        JsonNode? node = JsonNode.Parse("{\"name\":\"test\"}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(node).IsJsonValue());
    }

    [Test]
    public async Task HasJsonProperty_WhenPropertyExists_Passes()
    {
        JsonNode? node = JsonNode.Parse("{\"name\":\"Alice\"}");
        await Assert.That(node).HasJsonProperty("name");
    }

    [Test]
    public async Task HasJsonProperty_WhenPropertyMissing_Fails()
    {
        JsonNode? node = JsonNode.Parse("{\"name\":\"Alice\"}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(node).HasJsonProperty("missing"));
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_WhenPropertyMissing_Passes()
    {
        JsonNode? node = JsonNode.Parse("{\"name\":\"Alice\"}");
        await Assert.That(node).DoesNotHaveJsonProperty("missing");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_WhenPropertyExists_Fails()
    {
        JsonNode? node = JsonNode.Parse("{\"name\":\"Alice\"}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(node).DoesNotHaveJsonProperty("name"));
    }

#if NET8_0_OR_GREATER
    [Test]
    public async Task IsDeepEqualTo_WithIdenticalJson_Passes()
    {
        JsonNode? node1 = JsonNode.Parse("{\"name\":\"Alice\",\"age\":30}");
        JsonNode? node2 = JsonNode.Parse("{\"name\":\"Alice\",\"age\":30}");
        await Assert.That(node1).IsDeepEqualTo(node2);
    }

    [Test]
    public async Task IsDeepEqualTo_WithDifferentWhitespace_Passes()
    {
        JsonNode? node1 = JsonNode.Parse("{ \"name\" : \"Alice\" }");
        JsonNode? node2 = JsonNode.Parse("{\"name\":\"Alice\"}");
        await Assert.That(node1).IsDeepEqualTo(node2);
    }

    [Test]
    public async Task IsDeepEqualTo_WithDifferentJson_Fails()
    {
        JsonNode? node1 = JsonNode.Parse("{\"name\":\"Alice\"}");
        JsonNode? node2 = JsonNode.Parse("{\"name\":\"Bob\"}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(node1).IsDeepEqualTo(node2));
    }

    [Test]
    public async Task IsDeepEqualTo_ErrorMessageContainsPath()
    {
        JsonNode? node1 = JsonNode.Parse("{\"person\":{\"name\":\"Alice\",\"age\":30}}");
        JsonNode? node2 = JsonNode.Parse("{\"person\":{\"name\":\"Alice\",\"age\":31}}");

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(node1).IsDeepEqualTo(node2));

        await Assert.That(exception.Message).Contains("$.person.age");
    }

    [Test]
    public async Task IsNotDeepEqualTo_WithDifferentJson_Passes()
    {
        JsonNode? node1 = JsonNode.Parse("{\"name\":\"Alice\"}");
        JsonNode? node2 = JsonNode.Parse("{\"name\":\"Bob\"}");
        await Assert.That(node1).IsNotDeepEqualTo(node2);
    }

    [Test]
    public async Task IsNotDeepEqualTo_WithIdenticalJson_Fails()
    {
        JsonNode? node1 = JsonNode.Parse("{\"name\":\"Alice\"}");
        JsonNode? node2 = JsonNode.Parse("{\"name\":\"Alice\"}");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(node1).IsNotDeepEqualTo(node2));
    }
#endif
}
