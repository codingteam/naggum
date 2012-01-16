(*  Copyright (C) 2011 by ForNeVeR, Hagane

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
open Naggum.Reader
open Naggum.Runtime

open Context

//TODO: Shoot that putrid shit and throw it out.
let rec private generate (context : Context) (typeBuilder : TypeBuilder) (ilGen : ILGenerator) (form : SExp) =
    match form with
    | List list ->
        match list with
        | (Atom (Symbol "defun") :: Atom (Symbol name) :: List args :: body) ->
            let argsDef = Array.create (List.length args) typeof<obj>
            let methodGen = typeBuilder.DefineMethod(name, MethodAttributes.Public ||| MethodAttributes.Static, typeof<obj>, argsDef)
            let methodILGen = (methodGen.GetILGenerator())
            generateBody context typeBuilder methodILGen body
            methodILGen.Emit(OpCodes.Ret)
            // Add function to context:
            context.functions.[name] <- methodGen
        | Atom (Symbol "if") :: condition :: if_true :: if_false :: [] -> //full if form
            generate context typeBuilder ilGen condition
            let if_true_lbl = ilGen.DefineLabel()
            let end_form = ilGen.DefineLabel()
            ilGen.Emit (OpCodes.Brtrue, if_true_lbl)
            generate context typeBuilder ilGen if_false
            ilGen.Emit (OpCodes.Br,end_form)
            ilGen.MarkLabel if_true_lbl
            generate context typeBuilder ilGen if_true
            ilGen.MarkLabel end_form
        | Atom (Symbol "if") :: condition :: if_true :: [] -> //reduced if form
            generate context typeBuilder ilGen condition
            let if_true_lbl = ilGen.DefineLabel()
            let end_form = ilGen.DefineLabel()
            ilGen.Emit (OpCodes.Brtrue, if_true_lbl)
            ilGen.Emit OpCodes.Ldnull
            ilGen.Emit (OpCodes.Br,end_form)
            ilGen.MarkLabel if_true_lbl
            generate context typeBuilder ilGen if_true
            ilGen.MarkLabel end_form
        | Atom (Symbol "let") :: bindings :: body -> //let form
            ilGen.BeginScope()
            let scope_subctx = new Context (context)
            match bindings with
            | List list ->
                for binding in list do
                    match binding with
                    | List [(Atom (Symbol name)); form] ->
                        let local = ilGen.DeclareLocal(typeof<SExp>)
                        generate context typeBuilder ilGen form
                        ilGen.Emit (OpCodes.Stloc,local)
                        scope_subctx.locals.[name] <- Local local
                    | other -> failwithf "In let bindings: Expected: (name (form))\nGot: %A\n" other
            | other -> failwithf "In let form: expected: list of bindings\nGot: %A" other
            generateBody scope_subctx typeBuilder ilGen body
            ilGen.EndScope()
        | Atom (Symbol fname) :: args -> //generic funcall pattern
            genApply fname context typeBuilder ilGen args
        | _ -> failwithf "%A not supported yet." list
    | Atom a -> 
        let genf = new GeneratorFactory(typeBuilder) :> IGeneratorFactory
        let gen = genf.MakeGenerator context (Atom a)
        gen.Generate ilGen
    | other     -> failwithf "%A form not supported yet." other
and private generateBody context (typeBuilder : TypeBuilder) (ilGen : ILGenerator) (body : SExp list) =
    match body with
    | [] ->
        ilGen.Emit(OpCodes.Ldnull)
    | [last] ->
        generate context typeBuilder ilGen last
    | sexp :: rest ->
        generate context typeBuilder ilGen sexp
        ilGen.Emit(OpCodes.Pop)
        generateBody context typeBuilder ilGen rest
and private generateSeq context (typeBuilder : TypeBuilder) (ilGen : ILGenerator) (seq : SExp list) =
    match seq with
    | [] ->
        ilGen.Emit(OpCodes.Ldnull)
    | [last] ->
        generate context typeBuilder ilGen last
    | sexp :: rest ->
        generate context typeBuilder ilGen sexp
        generateBody context typeBuilder ilGen rest
and private genApply (funcName : string) (context : Context) (typeBuilder : TypeBuilder) (ilGen : ILGenerator) (argList: SExp list) : unit =
    try
        let func = context.functions.[funcName]
        generateSeq context typeBuilder ilGen argList
        ilGen.Emit(OpCodes.Call, func)
    with
    | :? KeyNotFoundException -> failwithf "Function %A not found." funcName

let private prologue (ilGen : ILGenerator) =
    ilGen.BeginScope()

let private epilogue context typeBuilder (ilGen : ILGenerator) =
    (* let argGetter = typeof<Value>.GetMethod "get_EmptyList"
    let isAtomGetter = typeof<SExp>.GetMethod "get_IsAtom"
    let atomItemGetter = typeof<SExp>.GetNestedType("Atom").GetMethod "get_Item"
    let isNumberGetter = typeof<Value>.GetMethod "get_IsNumber"
    let numberItemGetter = typeof<Value>.GetNestedType("Number").GetMethod "get_Item" *)

    //ilGen.Emit(OpCodes.Call, argGetter)
    genApply "main" context typeBuilder ilGen ([])
    (*
    // Analyze value returned from main:
    let sexp = ilGen.DeclareLocal(typeof<SExp>)
    let value = ilGen.DeclareLocal(typeof<Value>)

    let returnZero = ilGen.DefineLabel()
    let ``return`` = ilGen.DefineLabel()
    
    // Get SExp:
    ilGen.Emit(OpCodes.Castclass, typeof<SExp>)
    ilGen.Emit(OpCodes.Stloc, sexp.LocalIndex)

    // Check whether SExp is SExp.Atom:
    ilGen.Emit(OpCodes.Ldloc, sexp.LocalIndex)
    ilGen.Emit(OpCodes.Call, isAtomGetter)
    ilGen.Emit(OpCodes.Brfalse, returnZero)

    // Cast SExp to SExp.Atom:
    ilGen.Emit(OpCodes.Ldloc, sexp.LocalIndex)
    ilGen.Emit(OpCodes.Castclass, typeof<SExp>.GetNestedType("Atom"))
    
    // Get Value:
    ilGen.Emit(OpCodes.Call, atomItemGetter)
    ilGen.Emit(OpCodes.Stloc, value.LocalIndex)
    
    // Check whether Value is Number:
    ilGen.Emit(OpCodes.Ldloc, value.LocalIndex)
    ilGen.Emit(OpCodes.Call, isNumberGetter)
    ilGen.Emit(OpCodes.Brfalse, returnZero)
    
    // Cast Value to Number:
    ilGen.Emit(OpCodes.Ldloc, value.LocalIndex)
    ilGen.Emit(OpCodes.Castclass, typeof<Value>.GetNestedType("Number"))
    
    // Get float64 value:
    ilGen.Emit(OpCodes.Call, numberItemGetter)
 
    // Convert to int32:
    ilGen.Emit(OpCodes.Conv_I4)
    ilGen.Emit(OpCodes.Br, ``return``)
    
    ilGen.MarkLabel returnZero
    ilGen.Emit(OpCodes.Ldc_I4, 0)

    ilGen.MarkLabel ``return``
    *)
    ilGen.Emit OpCodes.Ret
    ilGen.EndScope()

let compile (source : StreamReader) (assemblyName : string) (fileName : string) : unit =
    let assemblyName = new AssemblyName(assemblyName)
    let assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save)
    let moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyBuilder.GetName().Name, fileName)
    let typeBuilder = moduleBuilder.DefineType("Program", TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.BeforeFieldInit)
    let methodBuilder = typeBuilder.DefineMethod("Main", MethodAttributes.Public ||| MethodAttributes.Static, typeof<int>, [| |])
    
    let gf = new GeneratorFactory(typeBuilder) :> IGeneratorFactory
    assemblyBuilder.SetEntryPoint methodBuilder
    
    let ilGenerator = methodBuilder.GetILGenerator()

    let context = Context.create ()
    prologue ilGenerator
    while not source.EndOfStream do
        let sexp = Reader.parse source
        let gen = gf.MakeGenerator context sexp
        gen.Generate ilGenerator
        //generate context typeBuilder ilGenerator sexp

    epilogue context typeBuilder ilGenerator

    typeBuilder.CreateType()
    |> ignore

    assemblyBuilder.Save fileName
