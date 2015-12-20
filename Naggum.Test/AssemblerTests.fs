module Naggum.Test.AssemblerTest

open System
open System.IO
open System.Text

open Xunit

open Naggum.Assembler
open Naggum.Assembler.Representation

let prepare (source : string) =
    use stream = new MemoryStream(Encoding.UTF8.GetBytes source)
    Assembler.prepare "file.ngi" stream

[<Fact>]
let ``Empty assembly should be processed`` () =
    let source = "(.assembly Empty)"
    let result = { Name = "Empty"; Units = List.empty }
    Assert.Equal ([result], prepare source)

[<Fact>]
let ``Hello world assembly should be processed`` () =
    let source = "(.assembly Hello
  (.method Main () (.entrypoint)
    (ldstr \"Hello, world!\")
    (call `void System.Console::WriteLine(string)`)
    (ret)))
"
    let result =
        { Name = "Empty"
          Units = [Method { Metadata = Set.singleton EntryPoint
                            Visibility = Public
                            Name = "Main"
                            ReturnType = typeof<Void> 
                            Body = [ Ldstr "Hello, world!"
                                     Call (typeof<Console>.GetMethod("WriteLine", [| typeof<String> |]))
                                     Ret ] } ] }
    Assert.Equal ([result], prepare source)
