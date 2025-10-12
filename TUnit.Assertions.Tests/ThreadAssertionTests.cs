using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class ThreadAssertionTests
{
    [Test]
    public async Task Test_Thread_IsAlive()
    {
        var currentThread = Thread.CurrentThread;
        await Assert.That(currentThread).IsAlive();
    }

    [Test]
    public async Task Test_Thread_IsAlive_NewThread()
    {
        var signal = new ManualResetEventSlim(false);
        Thread? thread = null;

        thread = new Thread(() =>
        {
            signal.Set();
            Thread.Sleep(100);
        });

        thread.Start();
        signal.Wait();

        try
        {
            await Assert.That(thread).IsAlive();
        }
        finally
        {
            thread.Join();
        }
    }

    [Test]
    public async Task Test_Thread_IsNotAlive()
    {
        var thread = new Thread(() => { });
        thread.Start();
        thread.Join();

        await Assert.That(thread).IsNotAlive();
    }

    [Test]
    public async Task Test_Thread_IsBackground()
    {
        var thread = new Thread(() => Thread.Sleep(100))
        {
            IsBackground = true
        };

        await Assert.That(thread).IsBackground();
    }

    [Test]
    public async Task Test_Thread_IsBackground_Started()
    {
        var thread = new Thread(() => Thread.Sleep(100))
        {
            IsBackground = true
        };

        thread.Start();

        try
        {
            await Assert.That(thread).IsBackground();
        }
        finally
        {
            thread.Join();
        }
    }

    [Test]
    public async Task Test_Thread_IsNotBackground()
    {
        var thread = new Thread(() => Thread.Sleep(100))
        {
            IsBackground = false
        };

        await Assert.That(thread).IsNotBackground();
    }

    [Test]
    public async Task Test_Thread_IsNotBackground_CurrentThread()
    {
        var currentThread = Thread.CurrentThread;
        await Assert.That(currentThread).IsNotBackground();
    }

    [Test]
    public async Task Test_Thread_IsThreadPoolThread()
    {
        Thread? capturedThread = null;

        await Task.Run(() =>
        {
            capturedThread = Thread.CurrentThread;
        });

        if (capturedThread != null)
        {
            await Assert.That(capturedThread).IsThreadPoolThread();
        }
    }

    [Test]
    public async Task Test_Thread_IsNotThreadPoolThread()
    {
        var currentThread = Thread.CurrentThread;
        await Assert.That(currentThread).IsNotThreadPoolThread();
    }

    [Test]
    public async Task Test_Thread_IsNotThreadPoolThread_NewThread()
    {
        var thread = new Thread(() => Thread.Sleep(50));
        thread.Start();

        try
        {
            await Assert.That(thread).IsNotThreadPoolThread();
        }
        finally
        {
            thread.Join();
        }
    }
}
