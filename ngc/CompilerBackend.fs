(*  Copyright (C) 2011 by ForNeVeR

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

module Naggum.CompilerBackend

open System
open System.Reflection
open System.Reflection.Emit

open Naggum.Context

/// Returns local variable index for Context variable.
let private prologue (ilGen : ILGenerator) =
    let constructorInfo = typeof<Context>.GetConstructor [| typeof<List<string * ContextItem>> |]
    
    ilGen.BeginScope ()
    let contextIndex = ilGen.DeclareLocal typeof<Context>
    
    let initIndex = ilGen.DeclareLocal typeof<List<string * ContextItem>>
    ilGen.Emit (OpCodes.Ldloc, initIndex)
    ilGen.Emit (OpCodes.Newobj, constructorInfo)
    
    ilGen.Emit (OpCodes.Stloc, contextIndex)
    
    contextIndex

let private epilogue (ilGen : ILGenerator) =
    ilGen.Emit OpCodes.Ret
    ilGen.EndScope ()

let compile (source : string) (assemblyName : string) (fileName : string) : unit =
    let assemblyName = new AssemblyName (assemblyName)
    let assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly (assemblyName, AssemblyBuilderAccess.Save)
    let moduleBuilder = assemblyBuilder.DefineDynamicModule (assemblyBuilder.GetName().Name, fileName)
    let typeBuilder = moduleBuilder.DefineType ("Program", TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.BeforeFieldInit)
    let methodBuilder = typeBuilder.DefineMethod ("Main", MethodAttributes.Public ||| MethodAttributes.Static, typeof<Void>, [| |])
    
    assemblyBuilder.SetEntryPoint methodBuilder
    
    let ilGenerator = methodBuilder.GetILGenerator()

    prologue ilGenerator
    epilogue ilGenerator

    typeBuilder.CreateType()
    |> ignore

    assemblyBuilder.Save (fileName, PortableExecutableKinds.Required32Bit, ImageFileMachine.I386)
