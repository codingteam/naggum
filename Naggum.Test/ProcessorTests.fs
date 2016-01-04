module Naggum.Test.ProcessorTests

open System
open System.IO
open System.Reflection
open System.Text

open Xunit

open Naggum.Assembler
open Naggum.Assembler.Representation

let mscorlib = Assembly.GetAssembly(typeof<Int32>)

let mainMethodDefinition =
    { Metadata = Set.singleton EntryPoint
      Visibility = Public
      Name = "Main"
      ArgumentTypes = List.empty
      ReturnType = typeof<Void> 
      Body = List.empty }

let consoleWriteLine =
    { Assembly = Some mscorlib
      ContainingType = Some typeof<Console>
      Name = "WriteLine"
      ArgumentTypes = [typeof<string>]
      ReturnType = typeof<Void> }

let checkPreparationResult (source : string) (expected : Assembly list) =
    use stream = new MemoryStream(Encoding.UTF8.GetBytes source)
    let actual = Processor.prepare "file.ngi" stream |> Seq.toList

    Assert.Equal (expected.ToString (), actual.ToString ()) // for diagnostic
    Assert.Equal<Assembly list> (expected, actual)

[<Fact>]
let ``Empty assembly should be processed`` () =
    let source = "(.assembly Empty)"
    let result = { Name = "Empty"; Units = List.empty }
    checkPreparationResult source [result]

[<Fact>]
let ``Simplest method should be processed`` () =
    let source = "(.assembly Stub
  (.method Main () System.Void (.entrypoint)
    (ret)))
"
    let result =
        { Name = "Stub"
          Units = [Method { mainMethodDefinition with 
                                Body = [ Ret ] } ] }
    checkPreparationResult source [result]

[<Fact>]
let ``Hello world assembly should be processed`` () =
    let source = "(.assembly Hello
  (.method Main () System.Void (.entrypoint)
    (ldstr \"Hello, world!\")
    (call (mscorlib System.Console WriteLine (System.String) System.Void))
    (ret)))
"
    let result =
        { Name = "Hello"
          Units = [Method { Metadata = Set.singleton EntryPoint
                            Visibility = Public
                            Name = "Main"
                            ArgumentTypes = List.empty
                            ReturnType = typeof<Void> 
                            Body = [ Ldstr "Hello, world!"
                                     Call consoleWriteLine
                                     Ret ] } ] }
    checkPreparationResult source [result]
