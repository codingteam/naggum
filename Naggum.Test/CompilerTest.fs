namespace Naggum.Test

open System.Diagnostics
open System.IO

open Xunit

open Naggum.Compiler

type CompilerTest() =
    static let testExtension = "naggum"
    static let resultExtension = "result"
    static let executableExtension = "exe"
    
    static let directory = @"..\..\..\tests"
    static let filenames = [@"comment"; @"test"]

    static member private RunTest testName =
        let basePath = Path.Combine(directory, testName)
        let testPath = Path.ChangeExtension(basePath, testExtension)
        let resultPath = Path.ChangeExtension(basePath, resultExtension)
        let executablePath = Path.ChangeExtension(testName, executableExtension)

        use stream = File.Open(testPath, FileMode.Open)
        Generator.compile stream testName executablePath []

        let startInfo = new ProcessStartInfo(executablePath, UseShellExecute = false, RedirectStandardOutput = true)
        let ``process`` = Process.Start startInfo
        ``process``.WaitForExit()
        let result = ``process``.StandardOutput.ReadToEnd().Replace("\r\n", "\n")

        let reference = (File.ReadAllText resultPath).Replace("\r\n", "\n")
        Assert.Equal(reference, result)

    [<Fact>]
    member this.RunTests() =
        filenames
        |> List.iter CompilerTest.RunTest
