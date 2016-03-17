module Naggum.Test.AssemblerTests

open System.IO
open System.Reflection.Emit
open System.Text

open Xunit

open Naggum.Assembler

let assemble (source : string) =
    use stream = new MemoryStream(Encoding.UTF8.GetBytes source)
    let repr = Processor.prepare "file.ngi" stream
    Assembler.assemble AssemblyBuilderAccess.Save (Seq.exactlyOne repr)

let execute source =
    let fileName = "file.exe"
    let assembly = assemble source
    assembly.Save fileName

    Process.run fileName

[<Fact>]
let ``Empty assembly should be assembled`` () =
    let source = "(.assembly Empty)"
    let result = assemble source
    Assert.NotNull result

[<Fact>]
let ``Hello world program should be executed`` () =
    let source = "(.assembly Hello
  (.method Main () System.Void (.entrypoint)
    (ldstr \"Hello, world!\")
    (call (mscorlib System.Console WriteLine (System.String) System.Void))
    (ret)))
"
    let output = execute source
    Assert.Equal ("Hello, world!\n", output)

[<Fact>]
let ``Sum program should be executed`` () =
    let source = "(.assembly Sum
  (.method Main () System.Void (.entrypoint)
    (ldc.i4 10)
    (ldc.i4 20)
    (ldc.i4 30)
    (add)
    (add)
    (call (mscorlib System.Console WriteLine (System.Int32) System.Void))
    (ret)))
"
    let output = execute source
    Assert.Equal ("60\n", output)
