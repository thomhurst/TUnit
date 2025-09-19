using System;
using System.Text;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class OtherAssertionTests
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
        var exception = new Exception("Test");
        await Assert.That(exception).HasNoInnerException();
    }

    [Test]
    public async Task Test_Exception_HasStackTrace()
    {
        Exception exception;
        try
        {
            throw new Exception("Test");
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        await Assert.That(exception).HasStackTrace();
    }

    [Test]
    public async Task Test_Exception_HasNoData()
    {
        var exception = new Exception("Test");
        await Assert.That(exception).HasNoData();
    }

    [Test]
    public async Task Test_StringBuilder_IsEmpty()
    {
        var sb = new StringBuilder();
        await Assert.That(sb).IsEmpty();
    }

    [Test]
    public async Task Test_StringBuilder_IsNotEmpty()
    {
        var sb = new StringBuilder("test");
        await Assert.That(sb).IsNotEmpty();
    }

    [Test]
    public async Task Test_StringBuilder_HasExcessCapacity()
    {
        var sb = new StringBuilder(100);
        sb.Append("test");
        await Assert.That(sb).HasExcessCapacity();
    }

    [Test]
    public async Task Test_DayOfWeek_IsWeekend()
    {
        var day = DayOfWeek.Saturday;
        await Assert.That(day).IsWeekend();
    }

    [Test]
    public async Task Test_DayOfWeek_IsWeekday()
    {
        var day = DayOfWeek.Monday;
        await Assert.That(day).IsWeekday();
    }

    [Test]
    public async Task Test_DayOfWeek_IsMonday()
    {
        var day = DayOfWeek.Monday;
        await Assert.That(day).IsMonday();
    }

    [Test]
    public async Task Test_DayOfWeek_IsFriday()
    {
        var day = DayOfWeek.Friday;
        await Assert.That(day).IsFriday();
    }

    [Test]
    public async Task Test_WeakReference_IsAlive()
    {
        var obj = new object();
        var weakRef = new WeakReference(obj);
        await Assert.That(weakRef).IsAlive();
        GC.KeepAlive(obj);
    }

    [Test]
    public async Task Test_WeakReference_IsDead()
    {
        var weakRef = new WeakReference(new object());
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await Assert.That(weakRef).IsDead();
    }
}