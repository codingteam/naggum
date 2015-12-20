module Naggum.Test.AssemblerTest

open System
open System.IO
open System.Text

open Xunit

open Naggum.Assembler
open Naggum.Assembler.Representation

let checkPreparationResult (source : string) (expected : Assembly list) =
    use stream = new MemoryStream(Encoding.UTF8.GetBytes source)
    let actual = Assembler.prepare "file.ngi" stream |> Seq.toList
    
    Assert.Equal<Assembly list> (expected, actual)

[<Fact>]
let ``Empty assembly should be processed`` () =
    let source = "(.assembly Empty)"
    let result = { Name = "Empty"; Units = List.empty }
    checkPreparationResult source [result]

[<Fact>]
let ``Simplest method should be processed`` () =
    let source = "(.assembly Stub
  (.method Main () (.entrypoint)
    (ret)))
"
    let result =
        { Name = "Stub"
          Units = [Method { Metadata = Set.singleton EntryPoint
                            Visibility = Public
                            Name = "Main"
                            ReturnType = typeof<Void> 
                            Body = [ Ret ] } ] }
    checkPreparationResult source [result]

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
    checkPreparationResult source [result]
