namespace TUnit.Core.Events;

public enum EventType
{
    Initialize,
    Dispose,
    TestRegistered,
    TestStart,
    TestEnd,
    TestSkipped,
    FirstTestInClass,
    FirstTestInAssembly,
    FirstTestInTestSession,
    LastTestInClass,
    LastTestInAssembly,
    LastTestInTestSession,
    TestRetry
}
