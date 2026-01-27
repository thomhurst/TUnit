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
        var startSignal = new ManualResetEventSlim(false);
        var completeSignal = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            startSignal.Set();
            completeSignal.Wait();
        });

        thread.Start();
        startSignal.Wait();

        try
        {
            await Assert.That(thread).IsAlive();
        }
        finally
        {
            completeSignal.Set();
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
        var startSignal = new ManualResetEventSlim(false);
        var completeSignal = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            startSignal.Set();
            completeSignal.Wait();
        })
        {
            IsBackground = true
        };

        thread.Start();
        startSignal.Wait();

        try
        {
            await Assert.That(thread).IsBackground();
        }
        finally
        {
            completeSignal.Set();
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
        // Create a new non-background thread (test threads run on background thread pool threads)
        var thread = new Thread(() => Thread.Sleep(50))
        {
            IsBackground = false
        };

        await Assert.That(thread).IsNotBackground();
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
        // Create a new non-pool thread (test threads run on thread pool threads)
        var thread = new Thread(() => Thread.Sleep(50));

        await Assert.That(thread).IsNotThreadPoolThread();
    }

    [Test]
    public async Task Test_Thread_IsNotThreadPoolThread_NewThread()
    {
        var startSignal = new ManualResetEventSlim(false);
        var completeSignal = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            startSignal.Set();
            completeSignal.Wait();
        });

        thread.Start();
        startSignal.Wait();

        try
        {
            await Assert.That(thread).IsNotThreadPoolThread();
        }
        finally
        {
            completeSignal.Set();
            thread.Join();
        }
    }
}
