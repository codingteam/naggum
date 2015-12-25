module Naggum.Test.AssemblerTests

open System.IO
open System.Text

open Xunit

open Naggum.Assembler

let assemble (source : string) =
    use stream = new MemoryStream(Encoding.UTF8.GetBytes source)
    let repr = Processor.prepare "file.ngi" stream
    let assemblies = Assembler.assemble repr
    List.ofSeq assemblies

let execute source =
    let fileName = "file.exe"
    let assembly = (Seq.exactlyOne << assemble) source
    assembly.Save fileName

    Process.run fileName

[<Fact>]
let ``Empty assembly should be assembled`` () =
    let source = "(.assembly Empty)"
    let result = assemble source
    Assert.Equal (1, result.Length)

[<Fact>]
let ``Hello world should be executed`` () =
    let source = "(.assembly Hello
  (.method Main () System.Void (.entrypoint)
    (ldstr \"Hello, world!\")
    (call (mscorlib System.Console WriteLine (System.String) System.Void))
    (ret)))
"
    let output = execute source
    Assert.Equal ("Hello, world!", output)
