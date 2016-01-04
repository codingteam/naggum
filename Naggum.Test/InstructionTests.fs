module Naggum.Test.InstructionTests

open System.IO
open System.Reflection.Emit
open System.Text

open Xunit

open Naggum.Assembler

let checkResult (body : string) (expectedResult : obj) =
    let source =
        sprintf
        <| "(.assembly Hello
  (.method Test () %s ()
    %s)
)"
        <| (expectedResult.GetType().FullName)
        <| body
    use stream = new MemoryStream(Encoding.UTF8.GetBytes source)
    let repr = Processor.prepare "file.ngi" stream |> Seq.exactlyOne
    let assembly = Assembler.assemble AssemblyBuilderAccess.RunAndCollect repr
    let ``module`` = assembly.GetModules () |> Seq.last // TODO: More proper check for *our* module. Why are there 2?
    let ``method`` = ``module``.GetMethod "Test"
    let result = ``method``.Invoke (null, [| |])
    Assert.Equal (expectedResult, result)

[<Fact>]
let ``Add instruction test`` () =
    checkResult "(ldc.i4 1) (ldc.i4 2) (add) (ret)" 3
    // TODO: Use FSCheck?
