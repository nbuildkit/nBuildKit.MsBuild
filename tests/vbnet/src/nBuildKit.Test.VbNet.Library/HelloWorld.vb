Imports System.Globalization
Imports System.Reflection

Public Class HelloWorld
    Private ReadOnly _name As String = AssemblyName()

    Private ReadOnly _version As String = AssemblyVersion()

    Public Function SayHello() As String
        Return String.Format( _
            CultureInfo.InvariantCulture, _
            "Hello world from: {0} [{1}]", _
            _name, _
            _version)
    End Function

    Private Function AssemblyName() As String
        Dim attr As AssemblyTitleAttribute = TryCast(Assembly.GetExecutingAssembly().GetCustomAttributes(GetType(AssemblyTitleAttribute), False)(0), AssemblyTitleAttribute)
        Return attr.Title
    End Function

    Private Function AssemblyVersion() As String
        Dim attr As AssemblyInformationalVersionAttribute = TryCast(Assembly.GetExecutingAssembly().GetCustomAttributes(GetType(AssemblyInformationalVersionAttribute), False)(0), AssemblyInformationalVersionAttribute)
        Return attr.InformationalVersion
    End Function
End Class
