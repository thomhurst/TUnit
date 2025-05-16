Imports System
Imports System.Collections.Generic
Imports TUnit.Core

Namespace Data

    Public Class DataGenerator
        Inherits DataSourceGeneratorAttribute(Of Integer, Integer, Integer)

        Public Overrides Iterator Function GenerateDataSources(dataGeneratorMetadata As DataGeneratorMetadata) As IEnumerable(Of Func(Of (Integer, Integer, Integer)))
            Yield Function() (1, 1, 2)
            Yield Function() (1, 2, 3)
            Yield Function() (4, 5, 9)
        End Function

    End Class

End Namespace
