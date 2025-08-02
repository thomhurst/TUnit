namespace TUnit.Core.Events;

[Flags]
public enum EventType
{
    Initialize = 1 << 0,
    Dispose = 1 << 1,
    TestRegistered = 1 << 2,
    TestStart = 1 << 3,
    TestEnd = 1 << 4,
    TestSkipped = 1 << 5,
    FirstTestInClass = 1 << 6,
    FirstTestInAssembly = 1 << 7,
    FirstTestInTestSession = 1 << 8,
    LastTestInClass = 1 << 9,
    LastTestInAssembly = 1 << 10,
    LastTestInTestSession = 1 << 11,
    TestRetry,
    All = Initialize
        | Dispose
        | TestRegistered
        | TestStart
        | TestEnd
        | TestSkipped
        | FirstTestInClass
        | FirstTestInAssembly
        | FirstTestInTestSession
        | LastTestInClass
        | LastTestInAssembly
        | LastTestInTestSession
        | TestRetry
}
