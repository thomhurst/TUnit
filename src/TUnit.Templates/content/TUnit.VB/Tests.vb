Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports TestProject.Data
Imports TUnit.Assertions
Imports TUnit.Assertions.Extensions
Imports TUnit.Core


Namespace TestProject

    Public Class Tests

        <Test>
        Public Sub Basic()
            Console.WriteLine("This is a basic test")
        End Sub

        <Test>
        <Arguments(1, 2, 3)>
        <Arguments(2, 3, 5)>
        Public Async Function DataDrivenArguments(a As Integer, b As Integer, c As Integer) As Task
            Console.WriteLine("This one can accept arguments from an attribute")

            Dim result = a + b

            Await Assert.That(result).IsEqualTo(c)
        End Function

        <Test>
        <MethodDataSource(NameOf(DataSource))>
        Public Async Function MethodDataSource(a As Integer, b As Integer, c As Integer) As Task
            Console.WriteLine("This one can accept arguments from a method")

            Dim result = a + b

            Await Assert.That(result).IsEqualTo(c)
        End Function

        <Test>
        <ClassDataSource(GetType(DataClass))>
        Public Sub ClassDataSourceTest(dataClass As DataClass)
            Console.WriteLine("This test can accept a class, which can also be pre-initialized before being injected in")
            Console.WriteLine("These can also be shared among other tests, or new'd up each time, by using the `Shared` property on the attribute")
        End Sub

        <Test>
        <DataGenerator>
        Public Async Function CustomDataGenerator(a As Integer, b As Integer, c As Integer) As Task
            Console.WriteLine("You can even define your own custom data generators")

            Dim result = a + b

            Await Assert.That(result).IsEqualTo(c)
        End Function

        Public Shared Iterator Function DataSource() As IEnumerable(Of (a As Integer, b As Integer, c As Integer))
            Yield (1, 1, 2)
            Yield (2, 1, 3)
            Yield (3, 1, 4)
        End Function

    End Class

End Namespace
