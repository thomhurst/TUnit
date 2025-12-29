using System.Xml.Linq;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class XDocumentAssertionTests
{
    [Test]
    public async Task HasRoot_WithRoot_Passes()
    {
        var doc = XDocument.Parse("<root><child/></root>");
        await Assert.That(doc).HasRoot();
    }

    [Test]
    public async Task HasRoot_WithoutRoot_Fails()
    {
        var doc = new XDocument();
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc).HasRoot());
    }

    [Test]
    public async Task DoesNotHaveRoot_WithoutRoot_Passes()
    {
        var doc = new XDocument();
        await Assert.That(doc).DoesNotHaveRoot();
    }

    [Test]
    public async Task DoesNotHaveRoot_WithRoot_Fails()
    {
        var doc = XDocument.Parse("<root/>");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc).DoesNotHaveRoot());
    }

    [Test]
    public async Task HasRootNamed_WithMatchingName_Passes()
    {
        var doc = XDocument.Parse("<person><name>Alice</name></person>");
        await Assert.That(doc).HasRootNamed("person");
    }

    [Test]
    public async Task HasRootNamed_WithMismatchedName_Fails()
    {
        var doc = XDocument.Parse("<person/>");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc).HasRootNamed("user"));
    }

    [Test]
    public async Task HasRootNamed_WithNoRoot_Fails()
    {
        var doc = new XDocument();
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc).HasRootNamed("root"));
    }

    [Test]
    public async Task HasDeclaration_WithDeclaration_Passes()
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("root"));
        await Assert.That(doc).HasDeclaration();
    }

    [Test]
    public async Task HasDeclaration_WithoutDeclaration_Fails()
    {
        var doc = XDocument.Parse("<root/>");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc).HasDeclaration());
    }

    [Test]
    public async Task DoesNotHaveDeclaration_WithoutDeclaration_Passes()
    {
        var doc = XDocument.Parse("<root/>");
        await Assert.That(doc).DoesNotHaveDeclaration();
    }

    [Test]
    public async Task IsDeepEqualTo_WithIdenticalDocuments_Passes()
    {
        var doc1 = XDocument.Parse("<root><child attr=\"value\">text</child></root>");
        var doc2 = XDocument.Parse("<root><child attr=\"value\">text</child></root>");
        await Assert.That(doc1).IsDeepEqualTo(doc2);
    }

    [Test]
    public async Task IsDeepEqualTo_WithDifferentWhitespace_Passes()
    {
        var doc1 = XDocument.Parse("<root>  <child />  </root>");
        var doc2 = XDocument.Parse("<root><child/></root>");
        await Assert.That(doc1).IsDeepEqualTo(doc2);
    }

    [Test]
    public async Task IsDeepEqualTo_WithDifferentContent_Fails()
    {
        var doc1 = XDocument.Parse("<root><child>Alice</child></root>");
        var doc2 = XDocument.Parse("<root><child>Bob</child></root>");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc1).IsDeepEqualTo(doc2));
    }

    [Test]
    public async Task IsDeepEqualTo_ErrorMessageContainsPath()
    {
        var doc1 = XDocument.Parse("<person><name>Alice</name><age>30</age></person>");
        var doc2 = XDocument.Parse("<person><name>Alice</name><age>31</age></person>");

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc1).IsDeepEqualTo(doc2));

        await Assert.That(exception.Message).Contains("/person/age");
    }

    [Test]
    public async Task IsNotDeepEqualTo_WithDifferentDocuments_Passes()
    {
        var doc1 = XDocument.Parse("<root><child>Alice</child></root>");
        var doc2 = XDocument.Parse("<root><child>Bob</child></root>");
        await Assert.That(doc1).IsNotDeepEqualTo(doc2);
    }

    [Test]
    public async Task IsNotDeepEqualTo_WithIdenticalDocuments_Fails()
    {
        var doc1 = XDocument.Parse("<root/>");
        var doc2 = XDocument.Parse("<root/>");
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(
            async () => await Assert.That(doc1).IsNotDeepEqualTo(doc2));
    }
}
