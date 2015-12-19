module Naggum.Compiler.Program

open System
open System.IO
open Naggum.Compiler.Generator

let args = List.ofArray (Environment.GetCommandLineArgs())
let mutable sources = []
let mutable asmRefs = []
for arg in (List.tail args) do
    if arg.StartsWith "/r:" then
        asmRefs <- arg.Replace("/r:","") :: asmRefs
    else
        sources <- arg :: sources
for fileName in sources do
    let source = File.Open (fileName,FileMode.Open) :> Stream
    let assemblyName = Path.GetFileNameWithoutExtension fileName
    Generator.compile source assemblyName (assemblyName + ".exe") asmRefs
    source.Close()
