using System.Xml.Linq;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class XElementAssertionTests
{
    [Test]
    public async Task HasName_WithMatchingName_Passes()
    {
        var element = new XElement("person");
        await Assert.That(element).HasName("person");
    }

    [Test]
    public async Task HasName_WithMismatchedName_Fails()
    {
        var element = new XElement("person");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).HasName("user"));
    }

    [Test]
    public async Task HasAttribute_WhenAttributeExists_Passes()
    {
        var element = new XElement("person", new XAttribute("name", "Alice"));
        await Assert.That(element).HasAttribute("name");
    }

    [Test]
    public async Task HasAttribute_WhenAttributeMissing_Fails()
    {
        var element = new XElement("person");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).HasAttribute("name"));
    }

    [Test]
    public async Task DoesNotHaveAttribute_WhenAttributeMissing_Passes()
    {
        var element = new XElement("person");
        await Assert.That(element).DoesNotHaveAttribute("name");
    }

    [Test]
    public async Task DoesNotHaveAttribute_WhenAttributeExists_Fails()
    {
        var element = new XElement("person", new XAttribute("name", "Alice"));
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).DoesNotHaveAttribute("name"));
    }

    [Test]
    public async Task HasAttributeValue_WithMatchingValue_Passes()
    {
        var element = new XElement("person", new XAttribute("name", "Alice"));
        await Assert.That(element).HasAttributeValue("name", "Alice");
    }

    [Test]
    public async Task HasAttributeValue_WithMismatchedValue_Fails()
    {
        var element = new XElement("person", new XAttribute("name", "Alice"));
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).HasAttributeValue("name", "Bob"));
    }

    [Test]
    public async Task HasAttributeValue_WhenAttributeMissing_Fails()
    {
        var element = new XElement("person");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).HasAttributeValue("name", "Alice"));
    }

    [Test]
    public async Task HasChildElement_WhenChildExists_Passes()
    {
        var element = new XElement("person", new XElement("name", "Alice"));
        await Assert.That(element).HasChildElement("name");
    }

    [Test]
    public async Task HasChildElement_WhenChildMissing_Fails()
    {
        var element = new XElement("person");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).HasChildElement("name"));
    }

    [Test]
    public async Task DoesNotHaveChildElement_WhenChildMissing_Passes()
    {
        var element = new XElement("person");
        await Assert.That(element).DoesNotHaveChildElement("name");
    }

    [Test]
    public async Task DoesNotHaveChildElement_WhenChildExists_Fails()
    {
        var element = new XElement("person", new XElement("name", "Alice"));
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).DoesNotHaveChildElement("name"));
    }

    [Test]
    public async Task HasValue_WithMatchingValue_Passes()
    {
        var element = new XElement("name", "Alice");
        await Assert.That(element).HasValue("Alice");
    }

    [Test]
    public async Task HasValue_WithMismatchedValue_Fails()
    {
        var element = new XElement("name", "Alice");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).HasValue("Bob"));
    }

    [Test]
    public async Task IsEmpty_WhenEmpty_Passes()
    {
        var element = new XElement("empty");
        await Assert.That(element).IsEmpty();
    }

    [Test]
    public async Task IsEmpty_WhenHasContent_Fails()
    {
        var element = new XElement("name", "Alice");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).IsEmpty());
    }

    [Test]
    public async Task IsNotEmpty_WhenHasContent_Passes()
    {
        var element = new XElement("name", "Alice");
        await Assert.That(element).IsNotEmpty();
    }

    [Test]
    public async Task IsNotEmpty_WhenEmpty_Fails()
    {
        var element = new XElement("empty");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).IsNotEmpty());
    }

    [Test]
    public async Task HasNamespace_WithMatchingNamespace_Passes()
    {
        XNamespace ns = "http://example.com";
        var element = new XElement(ns + "person");
        await Assert.That(element).HasNamespace("http://example.com");
    }

    [Test]
    public async Task HasNamespace_WithMismatchedNamespace_Fails()
    {
        XNamespace ns = "http://example.com";
        var element = new XElement(ns + "person");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).HasNamespace("http://other.com"));
    }

    [Test]
    public async Task HasChildCount_WithMatchingCount_Passes()
    {
        var element = new XElement("parent",
            new XElement("child1"),
            new XElement("child2"),
            new XElement("child3"));
        await Assert.That(element).HasChildCount(3);
    }

    [Test]
    public async Task HasChildCount_WithMismatchedCount_Fails()
    {
        var element = new XElement("parent",
            new XElement("child1"),
            new XElement("child2"));
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(element).HasChildCount(5));
    }

    [Test]
    public async Task IsDeepEqualTo_WithIdenticalElements_Passes()
    {
        var elem1 = XElement.Parse("<person name=\"Alice\"><age>30</age></person>");
        var elem2 = XElement.Parse("<person name=\"Alice\"><age>30</age></person>");
        await Assert.That(elem1).IsDeepEqualTo(elem2);
    }

    [Test]
    public async Task IsDeepEqualTo_WithDifferentElements_Fails()
    {
        var elem1 = XElement.Parse("<person name=\"Alice\"/>");
        var elem2 = XElement.Parse("<person name=\"Bob\"/>");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(elem1).IsDeepEqualTo(elem2));
    }

    [Test]
    public async Task IsDeepEqualTo_ErrorMessageContainsPath()
    {
        var elem1 = XElement.Parse("<person><details><age>30</age></details></person>");
        var elem2 = XElement.Parse("<person><details><age>31</age></details></person>");

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(elem1).IsDeepEqualTo(elem2));

        await Assert.That(exception.Message).Contains("/person/details/age");
    }

    [Test]
    public async Task IsNotDeepEqualTo_WithDifferentElements_Passes()
    {
        var elem1 = XElement.Parse("<person name=\"Alice\"/>");
        var elem2 = XElement.Parse("<person name=\"Bob\"/>");
        await Assert.That(elem1).IsNotDeepEqualTo(elem2);
    }

    [Test]
    public async Task IsNotDeepEqualTo_WithIdenticalElements_Fails()
    {
        var elem1 = XElement.Parse("<person/>");
        var elem2 = XElement.Parse("<person/>");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(elem1).IsNotDeepEqualTo(elem2));
    }
}
