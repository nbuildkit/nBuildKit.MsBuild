Imports NBuildKit.Test.VbNet.Library


Module Module1

    Sub Main()
        Dim helloWorld As HelloWorld = New HelloWorld
        System.Console.WriteLine(helloWorld.SayHello())
    End Sub

End Module
