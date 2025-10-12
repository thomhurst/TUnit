using System.IO;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class StreamAssertionTests
{
    [Test]
    public async Task Test_Stream_CanRead()
    {
        using Stream stream = new MemoryStream();
        await Assert.That(stream).CanRead();
    }

    [Test]
    public async Task Test_Stream_CannotRead()
    {
        using var memStream = new MemoryStream();
        using Stream writeOnlyStream = new StreamWrapper(memStream, canRead: false, canWrite: true, canSeek: true);
        await Assert.That(writeOnlyStream).CannotRead();
    }

    [Test]
    public async Task Test_Stream_CanWrite()
    {
        using Stream stream = new MemoryStream();
        await Assert.That(stream).CanWrite();
    }

    [Test]
    public async Task Test_Stream_CannotWrite()
    {
        var data = new byte[] { 1, 2, 3 };
        using Stream stream = new MemoryStream(data, writable: false);
        await Assert.That(stream).CannotWrite();
    }

    [Test]
    public async Task Test_Stream_CanSeek()
    {
        using Stream stream = new MemoryStream();
        await Assert.That(stream).CanSeek();
    }

    [Test]
    public async Task Test_Stream_CannotSeek()
    {
        using var memStream = new MemoryStream();
        using Stream nonSeekableStream = new StreamWrapper(memStream, canRead: true, canWrite: true, canSeek: false);
        await Assert.That(nonSeekableStream).CannotSeek();
    }

    [Test]
    public async Task Test_Stream_CanTimeout()
    {
        using var memStream = new MemoryStream();
        using Stream timeoutStream = new StreamWrapper(memStream, canRead: true, canWrite: true, canSeek: true, canTimeout: true);
        await Assert.That(timeoutStream).CanTimeout();
    }

    [Test]
    public async Task Test_Stream_CannotTimeout()
    {
        using Stream stream = new MemoryStream();
        await Assert.That(stream).CannotTimeout();
    }

    [Test]
    public async Task Test_Stream_IsAtStart()
    {
        using Stream stream = new MemoryStream(new byte[] { 1, 2, 3 });
        await Assert.That(stream).IsAtStart();
    }

    [Test]
    public async Task Test_Stream_IsAtEnd()
    {
        using Stream stream = new MemoryStream(new byte[] { 1, 2, 3 });
        stream.Position = stream.Length;
        await Assert.That(stream).IsAtEnd();
    }

    // Helper class to create streams with specific capabilities
    private class StreamWrapper : Stream
    {
        private readonly Stream _baseStream;
        private readonly bool _canRead;
        private readonly bool _canWrite;
        private readonly bool _canSeek;
        private readonly bool _canTimeout;

        public StreamWrapper(Stream baseStream, bool canRead, bool canWrite, bool canSeek, bool canTimeout = false)
        {
            _baseStream = baseStream;
            _canRead = canRead;
            _canWrite = canWrite;
            _canSeek = canSeek;
            _canTimeout = canTimeout;
        }

        public override bool CanRead => _canRead;
        public override bool CanWrite => _canWrite;
        public override bool CanSeek => _canSeek;
        public override bool CanTimeout => _canTimeout;
        public override long Length => _baseStream.Length;
        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public override void Flush() => _baseStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
        public override void SetLength(long value) => _baseStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);
    }
}
