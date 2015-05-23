Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Reflection
Imports NBuildKit.Test.VbNet.Library
Imports System.Globalization

<TestClass()> Public Class HelloWorldTest

    Private Function AssemblyName(a As Assembly) As String
        Dim attr As AssemblyTitleAttribute = TryCast(a.GetCustomAttribute(GetType(AssemblyTitleAttribute)), AssemblyTitleAttribute)
        Return attr.Title
    End Function

    Private Function AssemblyVersion(a As Assembly) As String
        Dim attr As AssemblyInformationalVersionAttribute = TryCast(a.GetCustomAttribute(GetType(AssemblyInformationalVersionAttribute)), AssemblyInformationalVersionAttribute)
        Return attr.InformationalVersion
    End Function

    <TestMethod()> Public Sub SayHello()
        Dim helloWorld As HelloWorld = New HelloWorld()
        Dim txt As String = helloWorld.SayHello()
        Dim expected As String = String.Format( _
            CultureInfo.InvariantCulture, _
            "Hello world from: {0} [{1}]", _
            AssemblyName(GetType(HelloWorld).Assembly), _
            AssemblyVersion(GetType(HelloWorld).Assembly))
        Assert.AreEqual(expected, txt)
    End Sub

End Class