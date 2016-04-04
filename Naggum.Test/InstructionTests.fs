module Naggum.Test.InstructionTests

open System.IO
open System.Reflection.Emit
open System.Text

open Xunit

open Naggum.Assembler

let checkResult (body : string) (expectedResult : obj) =
    // TODO: FSCheck tests
    let source =
        sprintf
        <| "(.assembly TestAssembly
  (.method Test () %s ()
    %s)
)"
        <| (expectedResult.GetType().FullName)
        <| body
    use stream = new MemoryStream(Encoding.UTF8.GetBytes source)
    let repr = Processor.prepare "file.ngi" stream |> Seq.exactlyOne
    let assembly = Assembler.assemble AssemblyBuilderAccess.RunAndCollect repr

    // There's also a dymanic manifest module in a dynamic assembly; we need to filter that.
    let ``module`` =
        assembly.GetModules ()
        |> Seq.filter (fun m -> m.ScopeName = assembly.GetName().Name)
        |> Seq.exactlyOne

    let ``method`` = ``module``.GetMethod "Test"
    let result = ``method``.Invoke (null, [| |])
    Assert.Equal (expectedResult, result)

[<Theory>]
[<InlineData("(ldc.i4 1) (ldc.i4 2) (add) (ret)", 3)>]
[<InlineData("(ldc.i4 10) (ldc.i4 20) (sub) (ret)", -10)>]
[<InlineData("(ldc.i4 5) (ldc.i4 30) (mul) (ret)", 150)>]
[<InlineData("(ldc.i4 9) (ldc.i4 4) (div) (ret)", 2)>]
let ``Integer math should work properly`` (code, result) =
    checkResult code result
