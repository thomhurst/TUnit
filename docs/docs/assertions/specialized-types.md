---
sidebar_position: 12
---

# Specialized Type Assertions

TUnit provides assertions for many specialized .NET types beyond the common primitives. This page covers GUID, HTTP, file system, networking, and other specialized assertions.

## GUID Assertions

### IsEmptyGuid / IsNotEmptyGuid

Tests whether a GUID is empty (`Guid.Empty`):

```csharp
[Test]
public async Task GUID_Is_Empty()
{
    var emptyGuid = Guid.Empty;
    await Assert.That(emptyGuid).IsEmptyGuid();

    var newGuid = Guid.NewGuid();
    await Assert.That(newGuid).IsNotEmptyGuid();
}
```

Practical usage:

```csharp
[Test]
public async Task Entity_Has_Valid_ID()
{
    var entity = new Entity { Id = Guid.NewGuid() };

    await Assert.That(entity.Id).IsNotEmptyGuid();
    await Assert.That(entity.Id).IsNotEqualTo(Guid.Empty);
}
```

## HTTP Status Code Assertions

### IsSuccess

Tests for 2xx success status codes:

```csharp
[Test]
public async Task HTTP_Success_Status()
{
    var response = await _client.GetAsync("/api/users");

    await Assert.That(response.StatusCode).IsSuccess();
}
```

Works with all 2xx codes:

```csharp
[Test]
public async Task Various_Success_Codes()
{
    await Assert.That(HttpStatusCode.OK).IsSuccess();                  // 200
    await Assert.That(HttpStatusCode.Created).IsSuccess();             // 201
    await Assert.That(HttpStatusCode.Accepted).IsSuccess();            // 202
    await Assert.That(HttpStatusCode.NoContent).IsSuccess();           // 204
}
```

### IsNotSuccess

```csharp
[Test]
public async Task HTTP_Not_Success()
{
    await Assert.That(HttpStatusCode.NotFound).IsNotSuccess();         // 404
    await Assert.That(HttpStatusCode.InternalServerError).IsNotSuccess(); // 500
}
```

### IsClientError

Tests for 4xx client error status codes:

```csharp
[Test]
public async Task HTTP_Client_Error()
{
    await Assert.That(HttpStatusCode.BadRequest).IsClientError();      // 400
    await Assert.That(HttpStatusCode.Unauthorized).IsClientError();    // 401
    await Assert.That(HttpStatusCode.Forbidden).IsClientError();       // 403
    await Assert.That(HttpStatusCode.NotFound).IsClientError();        // 404
}
```

### IsServerError

Tests for 5xx server error status codes:

```csharp
[Test]
public async Task HTTP_Server_Error()
{
    await Assert.That(HttpStatusCode.InternalServerError).IsServerError(); // 500
    await Assert.That(HttpStatusCode.BadGateway).IsServerError();          // 502
    await Assert.That(HttpStatusCode.ServiceUnavailable).IsServerError();  // 503
}
```

### IsRedirection

Tests for 3xx redirection status codes:

```csharp
[Test]
public async Task HTTP_Redirection()
{
    await Assert.That(HttpStatusCode.MovedPermanently).IsRedirection();    // 301
    await Assert.That(HttpStatusCode.Found).IsRedirection();               // 302
    await Assert.That(HttpStatusCode.TemporaryRedirect).IsRedirection();   // 307
}
```

## CancellationToken Assertions

### IsCancellationRequested / IsNotCancellationRequested

```csharp
[Test]
public async Task CancellationToken_Is_Requested()
{
    var cts = new CancellationTokenSource();
    cts.Cancel();

    await Assert.That(cts.Token).IsCancellationRequested();
}

[Test]
public async Task CancellationToken_Not_Requested()
{
    var cts = new CancellationTokenSource();

    await Assert.That(cts.Token).IsNotCancellationRequested();
}
```

### CanBeCanceled / CannotBeCanceled

```csharp
[Test]
public async Task Token_Can_Be_Canceled()
{
    var cts = new CancellationTokenSource();

    await Assert.That(cts.Token).CanBeCanceled();
}

[Test]
public async Task Default_Token_Cannot_Be_Canceled()
{
    var token = CancellationToken.None;

    await Assert.That(token).CannotBeCanceled();
}
```

## Character Assertions

### IsLetter / IsNotLetter

```csharp
[Test]
public async Task Char_Is_Letter()
{
    await Assert.That('A').IsLetter();
    await Assert.That('z').IsLetter();

    await Assert.That('5').IsNotLetter();
    await Assert.That('!').IsNotLetter();
}
```

### IsDigit / IsNotDigit

```csharp
[Test]
public async Task Char_Is_Digit()
{
    await Assert.That('0').IsDigit();
    await Assert.That('9').IsDigit();

    await Assert.That('A').IsNotDigit();
}
```

### IsWhiteSpace / IsNotWhiteSpace

```csharp
[Test]
public async Task Char_Is_WhiteSpace()
{
    await Assert.That(' ').IsWhiteSpace();
    await Assert.That('\t').IsWhiteSpace();
    await Assert.That('\n').IsWhiteSpace();

    await Assert.That('A').IsNotWhiteSpace();
}
```

### IsUpper / IsNotUpper

```csharp
[Test]
public async Task Char_Is_Upper()
{
    await Assert.That('A').IsUpper();
    await Assert.That('Z').IsUpper();

    await Assert.That('a').IsNotUpper();
}
```

### IsLower / IsNotLower

```csharp
[Test]
public async Task Char_Is_Lower()
{
    await Assert.That('a').IsLower();
    await Assert.That('z').IsLower();

    await Assert.That('A').IsNotLower();
}
```

### IsPunctuation / IsNotPunctuation

```csharp
[Test]
public async Task Char_Is_Punctuation()
{
    await Assert.That('.').IsPunctuation();
    await Assert.That(',').IsPunctuation();
    await Assert.That('!').IsPunctuation();

    await Assert.That('A').IsNotPunctuation();
}
```

## File System Assertions

### DirectoryInfo

#### Exists / DoesNotExist

```csharp
[Test]
public async Task Directory_Exists()
{
    var tempDir = new DirectoryInfo(Path.GetTempPath());

    await Assert.That(tempDir).Exists();
}

[Test]
public async Task Directory_Does_Not_Exist()
{
    var nonExistent = new DirectoryInfo(@"C:\NonExistentFolder");

    await Assert.That(nonExistent).DoesNotExist();
}
```

#### HasFiles / IsEmpty

```csharp
[Test]
public async Task Directory_Has_Files()
{
    var tempDir = new DirectoryInfo(Path.GetTempPath());

    // Likely has files
    await Assert.That(tempDir).HasFiles();
}

[Test]
public async Task Directory_Is_Empty()
{
    var emptyDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

    await Assert.That(emptyDir).IsEmpty();

    // Cleanup
    emptyDir.Delete();
}
```

#### HasSubdirectories / HasNoSubdirectories

```csharp
[Test]
public async Task Directory_Has_Subdirectories()
{
    var windowsDir = new DirectoryInfo(@"C:\Windows");

    await Assert.That(windowsDir).HasSubdirectories();
}
```

### FileInfo

#### Exists / DoesNotExist

```csharp
[Test]
public async Task File_Exists()
{
    var tempFile = Path.GetTempFileName();
    var fileInfo = new FileInfo(tempFile);

    await Assert.That(fileInfo).Exists();

    // Cleanup
    File.Delete(tempFile);
}

[Test]
public async Task File_Does_Not_Exist()
{
    var nonExistent = new FileInfo(@"C:\nonexistent.txt");

    await Assert.That(nonExistent).DoesNotExist();
}
```

#### IsReadOnly / IsNotReadOnly

```csharp
[Test]
public async Task File_Is_ReadOnly()
{
    var tempFile = Path.GetTempFileName();
    var fileInfo = new FileInfo(tempFile);

    fileInfo.IsReadOnly = true;
    await Assert.That(fileInfo).IsReadOnly();

    fileInfo.IsReadOnly = false;
    await Assert.That(fileInfo).IsNotReadOnly();

    // Cleanup
    File.Delete(tempFile);
}
```

#### IsHidden / IsNotHidden

```csharp
[Test]
public async Task File_Is_Hidden()
{
    var tempFile = Path.GetTempFileName();
    var fileInfo = new FileInfo(tempFile);

    fileInfo.Attributes |= FileAttributes.Hidden;
    await Assert.That(fileInfo).IsHidden();

    // Cleanup
    fileInfo.Attributes &= ~FileAttributes.Hidden;
    File.Delete(tempFile);
}
```

#### IsSystem / IsNotSystem

```csharp
[Test]
public async Task File_Is_System()
{
    // System files are typically in System32
    var systemFile = new FileInfo(@"C:\Windows\System32\kernel32.dll");

    if (systemFile.Exists)
    {
        await Assert.That(systemFile).IsSystem();
    }
}
```

#### IsExecutable / IsNotExecutable

```csharp
[Test]
public async Task File_Is_Executable()
{
    var exeFile = new FileInfo(@"C:\Windows\notepad.exe");

    if (exeFile.Exists)
    {
        await Assert.That(exeFile).IsExecutable();
    }
}
```

## IP Address Assertions

### IsIPv4 / IsNotIPv4

```csharp
[Test]
public async Task IP_Is_IPv4()
{
    var ipv4 = IPAddress.Parse("192.168.1.1");

    await Assert.That(ipv4).IsIPv4();
}

[Test]
public async Task IP_Not_IPv4()
{
    var ipv6 = IPAddress.Parse("::1");

    await Assert.That(ipv6).IsNotIPv4();
}
```

### IsIPv6 / IsNotIPv6

```csharp
[Test]
public async Task IP_Is_IPv6()
{
    var ipv6 = IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334");

    await Assert.That(ipv6).IsIPv6();
}

[Test]
public async Task IP_Not_IPv6()
{
    var ipv4 = IPAddress.Parse("127.0.0.1");

    await Assert.That(ipv4).IsNotIPv6();
}
```

## Lazy<T> Assertions

### IsValueCreated / IsNotValueCreated

```csharp
[Test]
public async Task Lazy_Value_Not_Created()
{
    var lazy = new Lazy<int>(() => 42);

    await Assert.That(lazy).IsNotValueCreated();

    var value = lazy.Value;

    await Assert.That(lazy).IsValueCreated();
    await Assert.That(value).IsEqualTo(42);
}
```

## Stream Assertions

### CanRead / CannotRead

```csharp
[Test]
public async Task Stream_Can_Read()
{
    using var stream = new MemoryStream();

    await Assert.That(stream).CanRead();
}
```

### CanWrite / CannotWrite

```csharp
[Test]
public async Task Stream_Can_Write()
{
    using var stream = new MemoryStream();

    await Assert.That(stream).CanWrite();
}

[Test]
public async Task Stream_Cannot_Write()
{
    var readOnlyStream = new MemoryStream(new byte[10], writable: false);

    await Assert.That(readOnlyStream).CannotWrite();
}
```

### CanSeek / CannotSeek

```csharp
[Test]
public async Task Stream_Can_Seek()
{
    using var stream = new MemoryStream();

    await Assert.That(stream).CanSeek();
}
```

### CanTimeout / CannotTimeout

```csharp
[Test]
public async Task Network_Stream_Can_Timeout()
{
    using var client = new TcpClient();
    // Note: stream only available after connection
    // await Assert.That(stream).CanTimeout();
}
```

## Process Assertions

### HasExited / HasNotExited

```csharp
[Test]
public async Task Process_Has_Not_Exited()
{
    var process = Process.Start("notepad.exe");

    await Assert.That(process).HasNotExited();

    process.Kill();
    process.WaitForExit();

    await Assert.That(process).HasExited();
}
```

### IsResponding / IsNotResponding

```csharp
[Test]
public async Task Process_Is_Responding()
{
    var process = Process.GetCurrentProcess();

    await Assert.That(process).IsResponding();
}
```

## Thread Assertions

### IsAlive / IsNotAlive

```csharp
[Test]
public async Task Thread_Is_Alive()
{
    var thread = new Thread(() => Thread.Sleep(1000));
    thread.Start();

    await Assert.That(thread).IsAlive();

    thread.Join();
    await Assert.That(thread).IsNotAlive();
}
```

### IsBackground / IsNotBackground

```csharp
[Test]
public async Task Thread_Is_Background()
{
    var thread = new Thread(() => { });
    thread.IsBackground = true;

    await Assert.That(thread).IsBackground();
}
```

### IsThreadPoolThread / IsNotThreadPoolThread

```csharp
[Test]
public async Task Check_ThreadPool_Thread()
{
    var currentThread = Thread.CurrentThread;

    // Test thread is typically not a thread pool thread
    await Assert.That(currentThread).IsNotThreadPoolThread();
}
```

## WeakReference Assertions

### IsAlive / IsNotAlive

```csharp
[Test]
public async Task WeakReference_Is_Alive()
{
    var obj = new object();
    var weakRef = new WeakReference(obj);

    await Assert.That(weakRef).IsAlive();

    obj = null!;
    GC.Collect();
    GC.WaitForPendingFinalizers();

    await Assert.That(weakRef).IsNotAlive();
}
```

## URI Assertions

### IsAbsoluteUri / IsNotAbsoluteUri

```csharp
[Test]
public async Task URI_Is_Absolute()
{
    var absolute = new Uri("https://example.com/path");

    await Assert.That(absolute).IsAbsoluteUri();
}

[Test]
public async Task URI_Is_Relative()
{
    var relative = new Uri("/path/to/resource", UriKind.Relative);

    await Assert.That(relative).IsNotAbsoluteUri();
}
```

## Encoding Assertions

### IsUtf8 / IsNotUtf8

```csharp
[Test]
public async Task Encoding_Is_UTF8()
{
    var encoding = Encoding.UTF8;

    await Assert.That(encoding).IsUtf8();
}

[Test]
public async Task Encoding_Not_UTF8()
{
    var encoding = Encoding.ASCII;

    await Assert.That(encoding).IsNotUtf8();
}
```

## Version Assertions

Version comparisons using standard comparison operators:

```csharp
[Test]
public async Task Version_Comparison()
{
    var v1 = new Version(1, 0, 0);
    var v2 = new Version(2, 0, 0);

    await Assert.That(v2).IsGreaterThan(v1);
    await Assert.That(v1).IsLessThan(v2);
}
```

## DayOfWeek Assertions

### IsWeekend / IsNotWeekend

```csharp
[Test]
public async Task Day_Is_Weekend()
{
    await Assert.That(DayOfWeek.Saturday).IsWeekend();
    await Assert.That(DayOfWeek.Sunday).IsWeekend();
}
```

### IsWeekday / IsNotWeekday

```csharp
[Test]
public async Task Day_Is_Weekday()
{
    await Assert.That(DayOfWeek.Monday).IsWeekday();
    await Assert.That(DayOfWeek.Tuesday).IsWeekday();
    await Assert.That(DayOfWeek.Wednesday).IsWeekday();
    await Assert.That(DayOfWeek.Thursday).IsWeekday();
    await Assert.That(DayOfWeek.Friday).IsWeekday();
}
```

## Practical Examples

### API Testing

```csharp
[Test]
public async Task API_Returns_Success()
{
    var response = await _client.GetAsync("/api/health");

    await Assert.That(response.StatusCode).IsSuccess();
    await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);
}
```

### File Upload Validation

```csharp
[Test]
public async Task Uploaded_File_Validation()
{
    var uploadedFile = new FileInfo("upload.txt");

    await Assert.That(uploadedFile).Exists();
    await Assert.That(uploadedFile).IsNotReadOnly();
    await Assert.That(uploadedFile.Length).IsGreaterThan(0);
}
```

### Configuration Directory Check

```csharp
[Test]
public async Task Config_Directory_Setup()
{
    var configDir = new DirectoryInfo(@"C:\ProgramData\MyApp");

    await Assert.That(configDir).Exists();
    await Assert.That(configDir).HasFiles();
}
```

### Network Validation

```csharp
[Test]
public async Task Server_IP_Is_Valid()
{
    var serverIp = IPAddress.Parse(Configuration["ServerIP"]);

    await Assert.That(serverIp).IsIPv4();
}
```

## See Also

- [Boolean](boolean.md) - For boolean properties of specialized types
- [String](string.md) - For string conversions and properties
- [Collections](collections.md) - For collections of specialized types
- [Types](types.md) - For type checking specialized types
