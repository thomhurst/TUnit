Imports TUnit.Assertions
Imports TUnit.Assertions.Extensions
Imports TUnit.Core

Public Class Tests
    <Test>
    Public Sub Test()
        TestContext.Current.OutputWriter.WriteLine("Test method executed")
    End Sub
    
#If Not NETFRAMEWORK Then
    <Test>
    Public Async Function TestAsync() As Task
        Dim result = 1 + 1
        Await Assert.That(result).IsNegative()
        Dim assertionResult = Await Assert.That(result).IsPositive()
        Console.WriteLine(assertionResult)
    End Function
#End If

End Class

