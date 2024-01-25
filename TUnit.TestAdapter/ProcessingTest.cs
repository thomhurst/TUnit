using TUnit.Core;

namespace TUnit.TestAdapter;

public record ProcessingTest(Test Test, object Class, Task Task);