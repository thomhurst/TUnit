using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/7301
// Members returning non-generic Task/ValueTask only had parameterless Returns/ReturnsAsync
// factories, so the returned task could not depend on the call's arguments.

public interface IIssue7301Repo
{
    Task SaveAsync(string u, CancellationToken ct = default);

    ValueTask SendAsync(string u, Func<string, ValueTask> send);
}

public class Issue7301Tests
{
    [Test]
    public async Task ReturnsAsync_Typed_Factory_Receives_Task_Member_Arguments()
    {
        string? seen = null;
        var mock = IIssue7301Repo.Mock();
        mock.SaveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsAsync((string u, CancellationToken ct) =>
        {
            seen = u;
            return Task.CompletedTask;
        });

        await mock.Object.SaveAsync("alice");

        await Assert.That(seen).IsEqualTo("alice");
    }

    [Test]
    public async Task Returns_Typed_Factory_Hands_Back_The_Task_Built_From_Arguments()
    {
        var tcs = new TaskCompletionSource<bool>();
        var mock = IIssue7301Repo.Mock();
        mock.SaveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string u, CancellationToken ct) => tcs.Task);

        var call = mock.Object.SaveAsync("bob");

        await Assert.That(call).IsSameReferenceAs((Task)tcs.Task);
        tcs.SetResult(true);
        await call;
    }

    [Test]
    public async Task Returns_Typed_Factory_Invokes_Callback_Argument_For_ValueTask_Member()
    {
        var sent = new List<string>();
        var mock = IIssue7301Repo.Mock();
        mock.SendAsync(Arg.Any<string>(), Arg.Any<Func<string, ValueTask>>()).Returns((string u, Func<string, ValueTask> send) => send(u));

        await mock.Object.SendAsync("carol", u =>
        {
            sent.Add(u);
            return default;
        });

        await Assert.That(sent).IsEquivalentTo(["carol"]);
    }

    [Test]
    public async Task Returns_Typed_Async_Lambda_Stays_Pending_Until_It_Completes()
    {
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var mock = IIssue7301Repo.Mock();
        mock.SendAsync(Arg.Any<string>(), Arg.Any<Func<string, ValueTask>>()).Returns(async (string u, Func<string, ValueTask> send) =>
        {
            await gate.Task;
            await send(u);
        });

        var sent = new List<string>();
        var call = mock.Object.SendAsync("dave", u =>
        {
            sent.Add(u);
            return default;
        }).AsTask();

        await Assert.That(call.IsCompleted).IsFalse();
        gate.SetResult(true);
        await call;
        await Assert.That(sent).IsEquivalentTo(["dave"]);
    }

    [Test]
    public async Task ReturnsAsync_Typed_Factory_Exception_Surfaces_From_The_Task()
    {
        var mock = IIssue7301Repo.Mock();
        mock.SaveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsAsync((string u, CancellationToken ct) => Task.FromException(new InvalidOperationException(u)));

        var exception = await Assert.That(async () => await mock.Object.SaveAsync("erin")).Throws<InvalidOperationException>();

        await Assert.That(exception!.Message).IsEqualTo("erin");
    }
}
