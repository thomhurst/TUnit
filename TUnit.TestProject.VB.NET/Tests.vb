Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports TUnit.Assertions
Imports TUnit.Assertions.AssertConditions.Throws
Imports TUnit.Assertions.Extensions
Imports TUnit.Core
Public Class Tests

    <Test>
    <Category("Pass")>
    Public Async Function ConsoleOutput() As Task
        Console.WriteLine("Blah!")
        Await Assert.That(TestContext.Current?.GetStandardOutput()).IsEqualTo("Blah!", StringComparison.Ordinal)
    End Function

    <Test>
    <Category("Pass")>
    Public Async Function Test1() As Task
        Dim value = "1"
        Await Assert.That(value).IsEqualTo("1")
    End Function

    <Test>
    <Category("Pass")>
    Public Async Function LessThan() As Task
        Dim value As Integer = 1
        Await Assert.That(value).IsLessThan(2)
    End Function

    <Test>
    <Category("Fail")>
    Public Async Function Test2() As Task
        Dim value = "2"
        Await Assert.That(value).IsEqualTo("1")
    End Function

    <Test>
    <Category("Pass")>
    Public Async Function Test3() As Task
        Await Task.Yield()
        Dim value = "1"
        Await Assert.That(value).IsEqualTo("1")
    End Function

    <Test>
    <Category("Fail")>
    Public Async Function Test4() As Task
        Await Task.Yield()
        Dim value = "2"
        Await Assert.That(value).IsEqualTo("1")
    End Function

    <Test>
    <Arguments("1")>
    <Arguments("2")>
    <Arguments("3")>
    <Arguments("4")>
    <Arguments("5")>
    <Category("Fail")>
    Public Async Function ParameterisedTests1(value As String) As Task
        Await Assert.That(value).IsEqualTo("1").And.HasLength().EqualTo(1)
    End Function

    <Test>
    <Arguments("1")>
    <Arguments("2")>
    <Arguments("3")>
    <Arguments("4")>
    <Arguments("5")>
    <Category("Fail")>
    Public Async Function ParameterisedTests2(value As String) As Task
        Await Task.Yield()
        Await Assert.That(value).IsEqualTo("1")
    End Function

    <Test, Skip("Reason1")>
    <Category("Skip")>
    Public Async Function Skip1() As Task
        Dim value = "1"
        Await Assert.That(value).IsEqualTo("1")
    End Function

    <Test, Skip("Reason2")>
    <Category("Skip")>
    Public Async Function Skip2() As Task
        Await Task.Yield()
        Dim value = "1"
        Await Assert.That(value).IsEqualTo("1")
    End Function

    <Test, CustomSkip>
    <Category("Skip")>
    Public Async Function CustomSkip1() As Task
        Await Task.Yield()
        Dim value = "1"
        Await Assert.That(value).IsEqualTo("1")
    End Function

    <Test>
    <MethodDataSource(NameOf(One))>
    <Category("Pass")>
    Public Async Function TestDataSource1(value As Integer) As Task
        Await Assert.That(value).IsEqualTo(1)
    End Function

    <Test>
    <MethodDataSource(NameOf(One))>
    <Category("Pass")>
    Public Async Function TestDataSource2(value As Integer) As Task
        Await Task.Yield()
        Await Assert.That(value).IsEqualTo(1)
    End Function

    <Test>
    <MethodDataSource(NameOf(Two))>
    <Category("Fail")>
    Public Async Function TestDataSource3(value As Integer) As Task
        Await Assert.That(value).IsEqualTo(1)
    End Function

    <Test>
    <MethodDataSource(NameOf(Two))>
    <Category("Fail")>
    Public Async Function TestDataSource4(value As Integer) As Task
        Await Task.Yield()
        Await Assert.That(value).IsEqualTo(1)
    End Function

    <Test>
    <MethodDataSource(GetType(TestDataSources), NameOf(One))>
    <Category("Pass")>
    Public Async Function TestDataSource5(value As Integer) As Task
        Await Assert.That(value).IsEqualTo(1)
    End Function

    <Test>
    <MethodDataSource(GetType(TestDataSources), NameOf(One))>
    <Category("Pass")>
    Public Async Function TestDataSource6(value As Integer) As Task
        Await Task.Yield()
        Await Assert.That(value).IsEqualTo(1)
    End Function

    <Test>
    <MethodDataSource(GetType(TestDataSources), "Two")>
    <Category("Pass")>
    Public Async Function TestDataSource_Wrong(value As Integer) As Task
        Await Assert.That(value).IsNotEqualTo(1)
    End Function

    <Test>
    <MethodDataSource(GetType(TestDataSources), NameOf(Two))>
    <Category("Fail")>
    Public Async Function TestDataSource7(value As Integer) As Task
        Await Assert.That(value).IsEqualTo(1)
    End Function

    <Test>
    <MethodDataSource(GetType(TestDataSources), NameOf(Two))>
    <Category("Fail")>
    Public Async Function TestDataSource8(value As Integer) As Task
        Await Task.Yield()
        Await Assert.That(value).IsEqualTo(1)
    End Function

    <Test>
    <Category("Pass")>
    Public Async Function TestContext1() As Task
        Await Assert.That(TestContext.Current?.TestDetails.TestName).IsEqualTo(NameOf(TestContext1))
    End Function

    <Test>
    <Category("Fail")>
    Public Async Function TestContext2() As Task
        Await Assert.That(TestContext.Current?.TestDetails.TestName).IsEqualTo(NameOf(TestContext1))
    End Function

    <Test>
    <Category("Fail")>
    Public Async Function Throws1() As Task
        Await Assert.That(Sub()
                              Dim s = New String({}) ' This will throw
                          End Sub).ThrowsException()
    End Function

    <Test>
    <Category("Fail")>
    Public Async Function Throws2() As Task
        Await Assert.That(Async Function()
                              Await Task.Yield()
                          End Function).ThrowsException()
    End Function

    <Test>
    <Category("Pass")>
    Public Async Function Throws3() As Task
        Await Assert.That(Sub() Throw New ApplicationException()).ThrowsException()
    End Function

    <Test>
    <Category("Pass")>
    Public Async Function Throws4() As Task
        Await Assert.That(Async Function()
                              Await Task.Yield()
                              Throw New ApplicationException()
                          End Function).ThrowsException()
    End Function

    <Test, Timeout(500)>
    <Category("Fail")>
    Public Async Function Timeout1(cancellationToken As CancellationToken) As Task
        Await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken)
    End Function

    <Test>
    <Category("Pass")>
    Public Async Function String_And_Condition() As Task
        Await Assert.That("1").IsEqualTo("1").And.HasLength().EqualTo(1)
    End Function

    <Test>
    <Category("Fail")>
    Public Async Function String_And_Condition2() As Task
        Await Assert.That("1").IsEqualTo("2").And.HasLength().EqualTo(2)
    End Function

    <Test>
    <Category("Pass")>
    Public Async Function Count1() As Task
        Dim list = New List(Of Integer) From {1, 2, 3}
        Await Assert.That(list).IsEquivalentTo({1, 2, 3}).And.HasCount().EqualTo(3)
    End Function

    <Test>
    <Category("Pass")>
    Public Async Function SingleItem() As Task
        Dim list = New List(Of Integer) From {1}
        Dim item = Await Assert.That(list).HasSingleItem()
        Await Assert.That(item).IsEqualTo(1)
    End Function

    <Test>
    <Category("Pass")>
    Public Async Function DistinctItems() As Task
        Dim list = New List(Of Integer) From {1, 2, 3, 4, 5}
        Await Assert.That(list).HasDistinctItems()
    End Function

    <Test>
    <Category("Pass")>
    Public Async Function Enumerable_NotEmpty() As Task
        Dim list = New List(Of Integer) From {1, 2, 3}
        Await Assert.That(list).IsNotEmpty()
    End Function

    <Test>
    <Category("Fail")>
    Public Async Function Count2() As Task
        Dim list = New List(Of Integer) From {1, 2, 3}
        Await Assert.That(list).IsEquivalentTo({1, 2, 3, 4, 5}).And.HasCount().EqualTo(5)
    End Function

    <Test>
    Public Async Function AssertMultiple() As Task
        Dim list = New List(Of Integer) From {1, 2, 3}
        Using Assert.Multiple()
            Await Assert.That(list).IsEquivalentTo({1, 2, 3, 4, 5})
            Await Assert.That(list).HasCount().EqualTo(5)
        End Using
    End Function

    <Test>
    Public Async Function NotNull() As Task
        Dim item As String = Nothing
        Await Assert.That(item).IsNotNull().And.IsNotEmpty()
    End Function

    <Test>
    Public Async Function NotNull2() As Task
        Dim item = ""
        Await Assert.That(item).IsNotNull().And.IsNotEmpty()
    End Function

    <Test>
    Public Async Function Assert_Multiple_With_Or_Conditions() As Task
        Dim one = ""
        Dim two = "Foo bar!"
        Using Assert.Multiple()
            Await Assert.That(one).IsNull().Or.IsEmpty()
            Await Assert.That(two).IsEqualTo("Foo bar").Or.IsNull()
        End Using
    End Function

    Private _retryCount As Integer = 0

    <Test>
    Public Async Function Throws5() As Task
        Await Task.CompletedTask
        Console.WriteLine(_retryCount)
        Throw New Exception()
    End Function

    <Test>
    Public Async Function Throws6() As Task
        Await Task.CompletedTask
        Console.WriteLine(_retryCount)
        Throw New Exception()
    End Function

    <Test>
    Public Sub Throws7()
        Console.WriteLine(_retryCount)
        Throw New Exception()
    End Sub

    <Test>
    Public Async Function OneSecond() As Task
        Await Task.Delay(TimeSpan.FromSeconds(1))
    End Function

    <Test>
    Public Async Function Long_String_Not_Equals() As Task
        Await Assert.That("ABCDEFGHIJKLMNOOPQRSTUVWXYZ").IsEqualTo("ABCDEFGHIJKLMNOPQRSTUVWXYZ", StringComparison.Ordinal)
    End Function

    <Test>
    Public Async Function Short_String_Not_Equals() As Task
        Await Assert.That("ABCCDE").IsEqualTo("ABCDE", StringComparison.Ordinal)
    End Function

    Public Shared Function One() As Integer
        Return 1
    End Function

    Public Shared Function Two() As Integer
        Return 2
    End Function

End Class
