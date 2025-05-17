' Here you could define global logic that would affect all tests

' You can use attributes at the assembly level to apply to all tests in the assembly
Imports TUnit.Core

<Assembly: Retry(3)>
<Assembly: CodeAnalysis.ExcludeFromCodeCoverage>

Public Class GlobalHooks

    <Before(HookType.TestSession)>
    Public Shared Sub SetUp()
        Console.WriteLine("Or you can define methods that do stuff before...")
    End Sub

    <After(HookType.TestSession)>
    Public Shared Sub CleanUp()
        Console.WriteLine("...and after!")
    End Sub

End Class