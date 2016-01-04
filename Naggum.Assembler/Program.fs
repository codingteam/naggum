module Naggum.Assembler.Program

open System
open System.IO
open System.Reflection
open System.Reflection.Emit

type private ReturnCode =
    | Success = 0
    | Error = 1
    | InvalidArguments = 2

let private printUsage () =
    let version = Assembly.GetExecutingAssembly().GetName().Version
    printfn "Naggum Assembler %A" version
    printfn "Usage: Naggum.Assembler [one or more file names]"

let private printError (error : Exception) =
    printfn "Error: %s" (error.ToString ())

let private save (assembly : AssemblyBuilder) =
    let name = assembly.FullName
    assembly.Save name
    printfn "Assembly %s saved" name

let private assemble fileName =
    use stream = File.OpenRead fileName
    let repr = Processor.prepare fileName stream
    let assemblies = Assembler.assemble repr
    assemblies |> Seq.iter save

let private nga =
    function
    | [| "--help" |] ->
        printUsage ()
        ReturnCode.Success
    | fileNames when fileNames.Length > 0 ->
        try
            fileNames |> Array.iter assemble
            ReturnCode.Success
        with
        | error ->
            printError error
            ReturnCode.Error
    | _ ->
        printUsage ()
        ReturnCode.InvalidArguments

[<EntryPoint>]
let main args =
    let result = nga args
    int result
