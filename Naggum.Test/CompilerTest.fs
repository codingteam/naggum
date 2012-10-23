namespace Naggum.Test
open Naggum.Compiler
open NUnit.Framework
open System.Diagnostics
open System.IO

[<TestFixture>]
type CompilerTest() =
    let sourceFilename = @"..\..\..\tests\test.naggum"

    [<Test>]
    member this.RunTest() =
        let filename = "test.exe"

        use stream = File.Open(sourceFilename, FileMode.Open)
        Generator.compile stream "test" filename []
        ignore <| (Process.Start filename).WaitForExit(30000) // 30 sec should be enough
