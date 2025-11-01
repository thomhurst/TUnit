using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class ExceptionAssertionTests
{
    [Test]
    public async Task Test_Exception_HasInnerException()
    {
        var innerException = new InvalidOperationException("Inner");
        var exception = new Exception("Outer", innerException);
        await Assert.That(exception).HasInnerException();
    }

    [Test]
    public async Task Test_Exception_HasNoInnerException()
    {
        var exception = new Exception("No inner");
        await Assert.That(exception).HasNoInnerException();
    }

    [Test]
    public async Task Test_Exception_HasStackTrace()
    {
        Exception? exception = null;
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        await Assert.That(exception!).HasStackTrace();
    }

    [Test]
    public async Task Test_Exception_HasNoData()
    {
        var exception = new Exception("No data");
        await Assert.That(exception).HasNoData();
    }

    [Test]
    public async Task Test_Exception_HasHelpLink()
    {
        var exception = new Exception("With help link")
        {
            HelpLink = "https://example.com/help"
        };
        await Assert.That(exception).HasHelpLink();
    }

    [Test]
    public async Task Test_Exception_HasNoHelpLink()
    {
        var exception = new Exception("No help link");
        await Assert.That(exception).HasNoHelpLink();
    }

    [Test]
    public async Task Test_Exception_HasSource()
    {
        var exception = new Exception("With source")
        {
            Source = "TestAssembly"
        };
        await Assert.That(exception).HasSource();
    }

    [Test]
    public async Task Test_Exception_HasNoSource()
    {
        var exception = new Exception("No source")
        {
            Source = null
        };
        await Assert.That(exception).HasNoSource();
    }

    // TODO: HasTargetSite and HasNoTargetSite assertions have been temporarily disabled
    // due to IL2026 trimming issues with Exception.TargetSite property
//    [Test]
//    public async Task Test_Exception_HasTargetSite()
//    {
//        Exception? exception = null;
//        try
//        {
//            ThrowException();
//        }
//        catch (Exception ex)
//        {
//            exception = ex;
//        }
//
//        await Assert.That(exception!).HasTargetSite();
//    }
//
//    [Test]
//    public async Task Test_Exception_HasNoTargetSite()
//    {
//        var exception = new Exception("No target site");
//        await Assert.That(exception).HasNoTargetSite();
//    }

    private static void ThrowException()
    {
        throw new InvalidOperationException("Test");
    }
}
