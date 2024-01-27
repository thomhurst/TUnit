using System.Collections.Concurrent;

namespace TUnit.Core;

public class TestContext
{
    private static readonly AsyncLocal<TestDetails> _asyncLocal = new();
    private static readonly ConcurrentDictionary<Guid, TestContext> _contexts = new();
    public static TestDetails Current
    {
        get => _asyncLocal.Value!;
        set => _asyncLocal.Value = value;
    }
}