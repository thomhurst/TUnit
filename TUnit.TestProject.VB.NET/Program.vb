Imports TUnit.Core

Public Class Tests
    <Test>
    Public Sub Test()
        TestContext.Current.OutputWriter.WriteLine("Test method executed")
    End Sub
End Class