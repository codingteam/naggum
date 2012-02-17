(*  Copyright (C) 2011-2012 by ForNeVeR, Hagane

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. *)

module Naggum.Compiler.Generator

open System
open System.IO
open System.Collections.Generic
open System.Reflection
open System.Reflection.Emit

open Naggum.Compiler.IGenerator
open Naggum.Compiler.GeneratorFactory
open Naggum.Compiler.Reader
open Naggum.Runtime

open Context

let private prologue (ilGen : ILGenerator) =
    ilGen.BeginScope()

let private epilogue context typeBuilder (ilGen : ILGenerator) =
    ilGen.Emit OpCodes.Ret
    ilGen.EndScope()

let compile (source : Stream) (assemblyName : string) (fileName : string) (asmRefs:string list): unit =
    let assemblyName = new AssemblyName(assemblyName)
    let assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save)
    let moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyBuilder.GetName().Name, fileName)
    let typeBuilder = moduleBuilder.DefineType("Program", TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.BeforeFieldInit)
    let methodBuilder = typeBuilder.DefineMethod("Main", MethodAttributes.Public ||| MethodAttributes.Static, typeof<int>, [| |])
    
    let gf = new GeneratorFactory(typeBuilder) :> IGeneratorFactory
    assemblyBuilder.SetEntryPoint methodBuilder
    
    let ilGenerator = methodBuilder.GetILGenerator()

    let context = Context.create ()

    //loading language runtime
    let rta = Assembly.LoadFrom("Naggum.Runtime.dll")
    context.loadAssembly rta

    List.iter context.loadAssembly (List.map Assembly.LoadFrom asmRefs)

    prologue ilGenerator
    try
        let body = Reader.parse fileName source
        let gen = gf.MakeBody context body
        gen.Generate ilGenerator
    with
    | ex -> printfn "File: %A\nForm: %A\nError: %A" fileName sexp ex.Source

    epilogue context typeBuilder ilGenerator

    typeBuilder.CreateType()
    |> ignore

    assemblyBuilder.Save fileName
