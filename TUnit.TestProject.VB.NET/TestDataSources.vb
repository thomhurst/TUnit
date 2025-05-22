Public Class TestDataSources

    Public Shared Function One() As Integer
        Return 1
    End Function

    Public Shared Function Two() As Integer
        Return 2
    End Function

    Public Shared Function OneEnumerable() As Integer()
        Return {1, 1, 1, 1, 1, 1, 1, 1, 1, 1}
    End Function

    Public Shared Function OneFailingEnumerable() As Integer()
        Return {1, 2, 3, 4, 5, 6, 7, 8, 9}
    End Function

End Class
