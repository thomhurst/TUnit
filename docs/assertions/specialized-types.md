# Specialized Type Assertions

TUnit provides assertions for many specialized .NET types beyond the common primitives. This page covers GUID, HTTP, file system, networking, and other specialized assertions.

## GUID Assertions[​](#guid-assertions "Direct link to GUID Assertions")

### IsEmptyGuid / IsNotEmptyGuid[​](#isemptyguid--isnotemptyguid "Direct link to IsEmptyGuid / IsNotEmptyGuid")

Tests whether a GUID is empty (`Guid.Empty`):

```
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

```
[Test]

public async Task Entity_Has_Valid_ID()

{

    var entity = new Entity { Id = Guid.NewGuid() };



    await Assert.That(entity.Id).IsNotEmptyGuid();

    await Assert.That(entity.Id).IsNotEqualTo(Guid.Empty);

}
```

## HTTP Status Code Assertions[​](#http-status-code-assertions "Direct link to HTTP Status Code Assertions")

### IsSuccess[​](#issuccess "Direct link to IsSuccess")

Tests for 2xx success status codes:

```
[Test]

public async Task HTTP_Success_Status()

{

    var response = await _client.GetAsync("/api/users");



    await Assert.That(response.StatusCode).IsSuccess();

}
```

Works with all 2xx codes:

```
[Test]

public async Task Various_Success_Codes()

{

    await Assert.That(HttpStatusCode.OK).IsSuccess();                  // 200

    await Assert.That(HttpStatusCode.Created).IsSuccess();             // 201

    await Assert.That(HttpStatusCode.Accepted).IsSuccess();            // 202

    await Assert.That(HttpStatusCode.NoContent).IsSuccess();           // 204

}
```

### IsNotSuccess[​](#isnotsuccess "Direct link to IsNotSuccess")

```
[Test]

public async Task HTTP_Not_Success()

{

    await Assert.That(HttpStatusCode.NotFound).IsNotSuccess();         // 404

    await Assert.That(HttpStatusCode.InternalServerError).IsNotSuccess(); // 500

}
```

### IsClientError[​](#isclienterror "Direct link to IsClientError")

Tests for 4xx client error status codes:

```
[Test]

public async Task HTTP_Client_Error()

{

    await Assert.That(HttpStatusCode.BadRequest).IsClientError();      // 400

    await Assert.That(HttpStatusCode.Unauthorized).IsClientError();    // 401

    await Assert.That(HttpStatusCode.Forbidden).IsClientError();       // 403

    await Assert.That(HttpStatusCode.NotFound).IsClientError();        // 404

}
```

### IsServerError[​](#isservererror "Direct link to IsServerError")

Tests for 5xx server error status codes:

```
[Test]

public async Task HTTP_Server_Error()

{

    await Assert.That(HttpStatusCode.InternalServerError).IsServerError(); // 500

    await Assert.That(HttpStatusCode.BadGateway).IsServerError();          // 502

    await Assert.That(HttpStatusCode.ServiceUnavailable).IsServerError();  // 503

}
```

### IsRedirection[​](#isredirection "Direct link to IsRedirection")

Tests for 3xx redirection status codes:

```
[Test]

public async Task HTTP_Redirection()

{

    await Assert.That(HttpStatusCode.MovedPermanently).IsRedirection();    // 301

    await Assert.That(HttpStatusCode.Found).IsRedirection();               // 302

    await Assert.That(HttpStatusCode.TemporaryRedirect).IsRedirection();   // 307

}
```

## CancellationToken Assertions[​](#cancellationtoken-assertions "Direct link to CancellationToken Assertions")

### IsCancellationRequested / IsNotCancellationRequested[​](#iscancellationrequested--isnotcancellationrequested "Direct link to IsCancellationRequested / IsNotCancellationRequested")

```
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

### CanBeCanceled / CannotBeCanceled[​](#canbecanceled--cannotbecanceled "Direct link to CanBeCanceled / CannotBeCanceled")

```
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

## Character Assertions[​](#character-assertions "Direct link to Character Assertions")

### IsLetter / IsNotLetter[​](#isletter--isnotletter "Direct link to IsLetter / IsNotLetter")

```
[Test]

public async Task Char_Is_Letter()

{

    await Assert.That('A').IsLetter();

    await Assert.That('z').IsLetter();



    await Assert.That('5').IsNotLetter();

    await Assert.That('!').IsNotLetter();

}
```

### IsDigit / IsNotDigit[​](#isdigit--isnotdigit "Direct link to IsDigit / IsNotDigit")

```
[Test]

public async Task Char_Is_Digit()

{

    await Assert.That('0').IsDigit();

    await Assert.That('9').IsDigit();



    await Assert.That('A').IsNotDigit();

}
```

### IsWhiteSpace / IsNotWhiteSpace[​](#iswhitespace--isnotwhitespace "Direct link to IsWhiteSpace / IsNotWhiteSpace")

```
[Test]

public async Task Char_Is_WhiteSpace()

{

    await Assert.That(' ').IsWhiteSpace();

    await Assert.That('\t').IsWhiteSpace();

    await Assert.That('\n').IsWhiteSpace();



    await Assert.That('A').IsNotWhiteSpace();

}
```

### IsUpper / IsNotUpper[​](#isupper--isnotupper "Direct link to IsUpper / IsNotUpper")

```
[Test]

public async Task Char_Is_Upper()

{

    await Assert.That('A').IsUpper();

    await Assert.That('Z').IsUpper();



    await Assert.That('a').IsNotUpper();

}
```

### IsLower / IsNotLower[​](#islower--isnotlower "Direct link to IsLower / IsNotLower")

```
[Test]

public async Task Char_Is_Lower()

{

    await Assert.That('a').IsLower();

    await Assert.That('z').IsLower();



    await Assert.That('A').IsNotLower();

}
```

### IsPunctuation / IsNotPunctuation[​](#ispunctuation--isnotpunctuation "Direct link to IsPunctuation / IsNotPunctuation")

```
[Test]

public async Task Char_Is_Punctuation()

{

    await Assert.That('.').IsPunctuation();

    await Assert.That(',').IsPunctuation();

    await Assert.That('!').IsPunctuation();



    await Assert.That('A').IsNotPunctuation();

}
```

## File System Assertions[​](#file-system-assertions "Direct link to File System Assertions")

### DirectoryInfo[​](#directoryinfo "Direct link to DirectoryInfo")

#### Exists / DoesNotExist[​](#exists--doesnotexist "Direct link to Exists / DoesNotExist")

```
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

#### HasFiles / IsEmpty[​](#hasfiles--isempty "Direct link to HasFiles / IsEmpty")

```
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

#### HasSubdirectories / HasNoSubdirectories[​](#hassubdirectories--hasnosubdirectories "Direct link to HasSubdirectories / HasNoSubdirectories")

```
[Test]

public async Task Directory_Has_Subdirectories()

{

    var windowsDir = new DirectoryInfo(@"C:\Windows");



    await Assert.That(windowsDir.EnumerateDirectories().Any()).IsTrue();

}
```

### FileInfo[​](#fileinfo "Direct link to FileInfo")

#### Exists / DoesNotExist[​](#exists--doesnotexist-1 "Direct link to Exists / DoesNotExist")

```
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

#### IsReadOnly / IsNotReadOnly[​](#isreadonly--isnotreadonly "Direct link to IsReadOnly / IsNotReadOnly")

```
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

#### IsHidden / IsNotHidden[​](#ishidden--isnothidden "Direct link to IsHidden / IsNotHidden")

```
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

#### IsSystem / IsNotSystem[​](#issystem--isnotsystem "Direct link to IsSystem / IsNotSystem")

```
[Test]

public async Task File_Is_System()

{

    // System files are typically in System32

    var systemFile = new FileInfo(@"C:\Windows\System32\kernel32.dll");



    if (systemFile.Exists)

    {

        await Assert.That(systemFile.Attributes.HasFlag(FileAttributes.System)).IsTrue();

    }

}
```

#### IsExecutable / IsNotExecutable[​](#isexecutable--isnotexecutable "Direct link to IsExecutable / IsNotExecutable")

```
[Test]

public async Task File_Is_Executable()

{

    var exeFile = new FileInfo(@"C:\Windows\notepad.exe");



    if (exeFile.Exists)

    {

        await Assert.That(exeFile.Extension).IsEqualTo(".exe");

    }

}
```

## IP Address Assertions[​](#ip-address-assertions "Direct link to IP Address Assertions")

### IsIPv4 / IsNotIPv4[​](#isipv4--isnotipv4 "Direct link to IsIPv4 / IsNotIPv4")

```
[Test]

public async Task IP_Is_IPv4()

{

    var ipv4 = IPAddress.Parse("192.168.1.1");



    await Assert.That(ipv4.AddressFamily).IsEqualTo(AddressFamily.InterNetwork);

}



[Test]

public async Task IP_Not_IPv4()

{

    var ipv6 = IPAddress.Parse("::1");



    await Assert.That(ipv6.AddressFamily).IsNotEqualTo(AddressFamily.InterNetwork);

}
```

### IsIPv6 / IsNotIPv6[​](#isipv6--isnotipv6 "Direct link to IsIPv6 / IsNotIPv6")

```
[Test]

public async Task IP_Is_IPv6()

{

    var ipv6 = IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334");



    await Assert.That(ipv6.AddressFamily).IsEqualTo(AddressFamily.InterNetworkV6);

}



[Test]

public async Task IP_Not_IPv6()

{

    var ipv4 = IPAddress.Parse("127.0.0.1");



    await Assert.That(ipv4.AddressFamily).IsNotEqualTo(AddressFamily.InterNetworkV6);

}
```

## Lazy\<T> Assertions[​](#lazyt-assertions "Direct link to Lazy<T> Assertions")

### IsValueCreated / IsNotValueCreated[​](#isvaluecreated--isnotvaluecreated "Direct link to IsValueCreated / IsNotValueCreated")

```
[Test]

public async Task Lazy_Value_Not_Created()

{

    var lazy = new Lazy<int>(() => 42);



    await Assert.That(lazy.IsValueCreated).IsFalse();



    var value = lazy.Value;



    await Assert.That(lazy).IsValueCreated();

    await Assert.That(value).IsEqualTo(42);

}
```

## Stream Assertions[​](#stream-assertions "Direct link to Stream Assertions")

### CanRead / CannotRead[​](#canread--cannotread "Direct link to CanRead / CannotRead")

```
[Test]

public async Task Stream_Can_Read()

{

    using var stream = new MemoryStream();



    await Assert.That((Stream) stream).CanRead();

}
```

### CanWrite / CannotWrite[​](#canwrite--cannotwrite "Direct link to CanWrite / CannotWrite")

```
[Test]

public async Task Stream_Can_Write()

{

    using var stream = new MemoryStream();



    await Assert.That((Stream) stream).CanWrite();

}



[Test]

public async Task Stream_Cannot_Write()

{

    var readOnlyStream = new MemoryStream(new byte[10], writable: false);



    await Assert.That((Stream) readOnlyStream).CannotWrite();

}
```

### CanSeek / CannotSeek[​](#canseek--cannotseek "Direct link to CanSeek / CannotSeek")

```
[Test]

public async Task Stream_Can_Seek()

{

    using var stream = new MemoryStream();



    await Assert.That((Stream) stream).CanSeek();

}
```

### CanTimeout / CannotTimeout[​](#cantimeout--cannottimeout "Direct link to CanTimeout / CannotTimeout")

```
[Test]

public async Task Network_Stream_Can_Timeout()

{

    using var client = new TcpClient();

    // Note: stream only available after connection

    // await Assert.That(stream).CanTimeout();

}
```

## Process Assertions[​](#process-assertions "Direct link to Process Assertions")

### HasExited / HasNotExited[​](#hasexited--hasnotexited "Direct link to HasExited / HasNotExited")

```
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

### IsResponding / IsNotResponding[​](#isresponding--isnotresponding "Direct link to IsResponding / IsNotResponding")

```
[Test]

public async Task Process_Is_Responding()

{

    var process = Process.GetCurrentProcess();



    await Assert.That(process.Responding).IsTrue();

}
```

## Thread Assertions[​](#thread-assertions "Direct link to Thread Assertions")

### IsAlive / IsNotAlive[​](#isalive--isnotalive "Direct link to IsAlive / IsNotAlive")

```
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

### IsBackground / IsNotBackground[​](#isbackground--isnotbackground "Direct link to IsBackground / IsNotBackground")

```
[Test]

public async Task Thread_Is_Background()

{

    var thread = new Thread(() => { });

    thread.IsBackground = true;



    await Assert.That(thread).IsBackground();

}
```

### IsThreadPoolThread / IsNotThreadPoolThread[​](#isthreadpoolthread--isnotthreadpoolthread "Direct link to IsThreadPoolThread / IsNotThreadPoolThread")

```
[Test]

public async Task Check_ThreadPool_Thread()

{

    var currentThread = Thread.CurrentThread;



    // Test thread is typically not a thread pool thread

    await Assert.That(currentThread).IsNotThreadPoolThread();

}
```

## WeakReference Assertions[​](#weakreference-assertions "Direct link to WeakReference Assertions")

### IsAlive / IsNotAlive[​](#isalive--isnotalive-1 "Direct link to IsAlive / IsNotAlive")

```
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

## URI Assertions[​](#uri-assertions "Direct link to URI Assertions")

### IsAbsoluteUri / IsNotAbsoluteUri[​](#isabsoluteuri--isnotabsoluteuri "Direct link to IsAbsoluteUri / IsNotAbsoluteUri")

```
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

## Encoding Assertions[​](#encoding-assertions "Direct link to Encoding Assertions")

### IsUtf8 / IsNotUtf8[​](#isutf8--isnotutf8 "Direct link to IsUtf8 / IsNotUtf8")

```
[Test]

public async Task Encoding_Is_UTF8()

{

    var encoding = Encoding.UTF8;



    await Assert.That(encoding.WebName).IsEqualTo(Encoding.UTF8.WebName);

}



[Test]

public async Task Encoding_Not_UTF8()

{

    var encoding = Encoding.ASCII;



    await Assert.That(encoding.WebName).IsNotEqualTo(Encoding.UTF8.WebName);

}
```

## Version Assertions[​](#version-assertions "Direct link to Version Assertions")

Version comparisons using standard comparison operators:

```
[Test]

public async Task Version_Comparison()

{

    var v1 = new Version(1, 0, 0);

    var v2 = new Version(2, 0, 0);



    await Assert.That(v2).IsGreaterThan(v1);

    await Assert.That(v1).IsLessThan(v2);

}
```

## DayOfWeek Assertions[​](#dayofweek-assertions "Direct link to DayOfWeek Assertions")

### IsWeekend / IsNotWeekend[​](#isweekend--isnotweekend "Direct link to IsWeekend / IsNotWeekend")

```
[Test]

public async Task Day_Is_Weekend()

{

    await Assert.That(DayOfWeek.Saturday).IsWeekend();

    await Assert.That(DayOfWeek.Sunday).IsWeekend();

}
```

### IsWeekday / IsNotWeekday[​](#isweekday--isnotweekday "Direct link to IsWeekday / IsNotWeekday")

```
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

## Practical Examples[​](#practical-examples "Direct link to Practical Examples")

### API Testing[​](#api-testing "Direct link to API Testing")

```
[Test]

public async Task API_Returns_Success()

{

    var response = await _client.GetAsync("/api/health");



    await Assert.That(response.StatusCode).IsSuccess();

    await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.InternalServerError);

}
```

### File Upload Validation[​](#file-upload-validation "Direct link to File Upload Validation")

```
[Test]

public async Task Uploaded_File_Validation()

{

    var uploadedFile = new FileInfo("upload.txt");



    await Assert.That(uploadedFile).Exists();

    await Assert.That(uploadedFile).IsNotReadOnly();

    await Assert.That(uploadedFile.Length).IsGreaterThan(0);

}
```

### Configuration Directory Check[​](#configuration-directory-check "Direct link to Configuration Directory Check")

```
[Test]

public async Task Config_Directory_Setup()

{

    var configDir = new DirectoryInfo(@"C:\ProgramData\MyApp");



    await Assert.That(configDir).Exists();

    await Assert.That(configDir).HasFiles();

}
```

### Network Validation[​](#network-validation "Direct link to Network Validation")

```
[Test]

public async Task Server_IP_Is_Valid()

{

    var serverIp = IPAddress.Parse(Configuration["ServerIP"] ?? "127.0.0.1");



    await Assert.That(serverIp.AddressFamily).IsEqualTo(AddressFamily.InterNetwork);

}
```

## See Also[​](#see-also "Direct link to See Also")

* [Boolean](/docs/assertions/boolean.md) - For boolean properties of specialized types
* [String](/docs/assertions/string.md) - For string conversions and properties
* [Collections](/docs/assertions/collections.md) - For collections of specialized types
* [Types](/docs/assertions/types.md) - For type checking specialized types
