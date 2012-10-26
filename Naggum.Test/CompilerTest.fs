(*  Copyright (C) 2012 by ForNeVeR

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. *)

namespace Naggum.Test
open Naggum.Compiler
open NUnit.Framework
open System.Diagnostics
open System.IO

[<TestFixture>]
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
        let result = ``process``.StandardOutput.ReadToEnd()

        let reference = File.ReadAllText resultPath
        Assert.AreEqual(reference, result)

    [<Test>]
    member this.RunTests() =
        filenames
        |> List.iter CompilerTest.RunTest    
