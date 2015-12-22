module Naggum.Test.AssemblerTests

open System
open System.IO
open System.Reflection
open System.Text

open Xunit

open Naggum.Assembler
open Naggum.Assembler.Representation

let assemble (source : string) =
    use stream = new MemoryStream(Encoding.UTF8.GetBytes source)
    let repr = Processor.prepare "file.ngi" stream
    let assemblies = Assembler.assemble repr
    Array.ofSeq assemblies

[<Fact>]
let ``Empty assembly should be assembled`` () =
    let source = "(.assembly Empty)"
    let result = assemble source
    Assert.Equal (1, result.Length)

// TODO: Additional integration tests
