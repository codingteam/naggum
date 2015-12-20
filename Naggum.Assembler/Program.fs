module Naggum.Assembler.Program

open System.Reflection

let printUsage () =
    let version = Assembly.GetExecutingAssembly().GetName().Version
    printfn "Naggum Assembler %A" version

[<EntryPoint>]
let main _ = 
    printUsage ()
    0
