Imports TUnit.Core

Public Class CustomSkipAttribute
    Inherits SkipAttribute

    Public Sub New()
        MyBase.New("Some reason")
    End Sub
End Class
