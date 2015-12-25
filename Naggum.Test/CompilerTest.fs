namespace Naggum.Test

open System
open System.Diagnostics
open System.IO

open Xunit

open Naggum.Compiler

type CompilerTest() =
    static let testExtension = "naggum"
    static let resultExtension = "result"
    static let executableExtension = "exe"
    
    static let directory = Path.Combine ("..", "..", "..", "tests")
    static let filenames = [@"comment"; @"test"]

    static member private RunTest testName =
        let basePath = Path.Combine(directory, testName)
        let testPath = Path.ChangeExtension(basePath, testExtension)
        let resultPath = Path.ChangeExtension(basePath, resultExtension)
        let executableName = Path.ChangeExtension (testName, executableExtension)
        let executablePath = Path.Combine (Environment.CurrentDirectory, executableName)

        use stream = File.Open(testPath, FileMode.Open)
        Generator.compile stream testName executablePath []

        let result = Process.run executablePath

        let reference = (File.ReadAllText resultPath).Replace("\r\n", "\n")
        Assert.Equal(reference, result)

    [<Fact>]
    member this.RunTests() =
        filenames
        |> List.iter CompilerTest.RunTest
