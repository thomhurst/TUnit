using System.Net;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class HttpStatusCodeAssertionTests
{
    [Test]
    public async Task Test_HttpStatusCode_IsSuccess()
    {
        var statusCode = HttpStatusCode.OK;
        await Assert.That(statusCode).IsSuccess();
    }

    [Test]
    public async Task Test_HttpStatusCode_IsNotSuccess()
    {
        var statusCode = HttpStatusCode.NotFound;
        await Assert.That(statusCode).IsNotSuccess();
    }

    [Test]
    public async Task Test_HttpStatusCode_IsClientError()
    {
        var statusCode = HttpStatusCode.BadRequest;
        await Assert.That(statusCode).IsClientError();
    }

    [Test]
    public async Task Test_HttpStatusCode_IsServerError()
    {
        var statusCode = HttpStatusCode.InternalServerError;
        await Assert.That(statusCode).IsServerError();
    }

    [Test]
    public async Task Test_HttpStatusCode_IsRedirection()
    {
        var statusCode = HttpStatusCode.MovedPermanently;
        await Assert.That(statusCode).IsRedirection();
    }

    [Test]
    public async Task Test_HttpStatusCode_IsInformational()
    {
        var statusCode = HttpStatusCode.Continue;
        await Assert.That(statusCode).IsInformational();
    }

    [Test]
    public async Task Test_HttpStatusCode_IsError()
    {
        var statusCode = HttpStatusCode.BadGateway;
        await Assert.That(statusCode).IsError();
    }
}