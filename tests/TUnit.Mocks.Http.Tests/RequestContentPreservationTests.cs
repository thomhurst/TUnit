using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace TUnit.Mocks.Http.Tests;

public class RequestContentPreservationTests
{
    [Test]
    [Arguments("binary")]
    [Arguments("text")]
    [Arguments("multipart")]
    public async Task ResponseFactoryReceivesOriginalContent(string contentKind)
    {
        using var handler = new MockHttpHandler();
        using var client = handler.CreateClient("https://example.test");
        using var content = CreateContent(contentKind);
        content.Headers.Add("X-Body-Tag", "original");
        var originalBytes = await content.ReadAsByteArrayAsync();
        var originalContentType = content.Headers.ContentType?.ToString();
        HttpContent? factoryContent = null;

        handler.OnPost("/upload").RespondWith(request =>
        {
            factoryContent = request.Content;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var response = await client.PostAsync("/upload", content);
        var factoryBytes = await factoryContent!.ReadAsByteArrayAsync();

        using (Assert.Multiple())
        {
            await Assert.That(Convert.ToHexString(factoryBytes)).IsEqualTo(Convert.ToHexString(originalBytes));
            await Assert.That(factoryContent.Headers.ContentType?.ToString()).IsEqualTo(originalContentType);
            await Assert.That(factoryContent.Headers.Contains("X-Body-Tag")).IsTrue();
            await Assert.That(factoryContent).IsSameReferenceAs(content);
        }
    }

    [Test]
    public async Task ResponseFactoryCanRereadNonSeekableContent()
    {
        using var handler = new MockHttpHandler();
        using var client = handler.CreateClient("https://example.test");
        byte[] bytes = [0x00, 0xff, 0x80, 0x01];
        using var stream = new NonSeekableStream(bytes);
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        Task<byte[]>? factoryRead = null;

        handler.OnPost("/upload").RespondWith(request =>
        {
            factoryRead = request.Content!.ReadAsByteArrayAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var response = await client.PostAsync("/upload", content);
        var factoryBytes = await factoryRead!;

        await Assert.That(Convert.ToHexString(factoryBytes)).IsEqualTo(Convert.ToHexString(bytes));
    }

    private static HttpContent CreateContent(string contentKind)
    {
        return contentKind switch
        {
            "binary" => new ByteArrayContent([0x00, 0xff, 0x80, 0x01]),
            "text" => new StringContent("café", Encoding.Unicode, "text/plain"),
            "multipart" => new MultipartFormDataContent("test-boundary")
            {
                { new ByteArrayContent([0x00, 0xff, 0x80, 0x01]), "file", "upload.bin" }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(contentKind))
        };
    }

    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    }
}
