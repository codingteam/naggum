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
open System.Collections.Generic
open System.Reflection
open System.Reflection.Emit

open Naggum.Reader
open Naggum.Runtime
open Naggum.Types

open Context

let private prologue (ilGen : ILGenerator) =
    ilGen.BeginScope()

let private genApply (funcName : string) (context : Context) (ilGen : ILGenerator) : unit =
    let func = context.functions.[funcName]
    ilGen.Emit(OpCodes.Call, func)

let private epilogue context (ilGen : ILGenerator) =
    let argGetter = typeof<Value>.GetMethod "get_EmptyList"

    ilGen.Emit(OpCodes.Call, argGetter)
    genApply "main" context ilGen

    ilGen.Emit OpCodes.Ret
    ilGen.EndScope()
//TODO: Split this to generate_if, generate_let, generate_etc
let rec private generate (context : Context) (typeBuilder : TypeBuilder) (ilGen : ILGenerator) (form : SExp) =
    match form with
    | List list ->
        match list with
        | (Atom (Symbol "defun") :: Atom (Symbol name) :: List args :: body) ->
            let argsDef = Array.create (List.length args) typeof<obj>
            let methodGen = typeBuilder.DefineMethod(name, MethodAttributes.Public ||| MethodAttributes.Static, typeof<obj>, argsDef)
            generateBody context typeBuilder (methodGen.GetILGenerator()) body

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
            //TODO: Check for errors here
            match bindings with
            | List list ->
                for binding in list do
                    match binding with
                    | List [(Atom (Symbol name)); form] ->
                        let local = ilGen.DeclareLocal(typeof<SExp>)
                        generate context typeBuilder ilGen form
                        ilGen.Emit (OpCodes.Stloc,local)
                        scope_subctx.locals.[name] <- local
            generateBody scope_subctx typeBuilder ilGen body
            ilGen.EndScope()
        | _ -> failwithf "%A not supported yet." list
    | Atom a -> 
        let atomCons = typeof<SExp>.GetMethod "NewAtom"
        pushValue context ilGen a
        ilGen.Emit(OpCodes.Call, atomCons)
    | other     -> failwithf "%A form not supported yet." other
and private generateBody context (typeBuilder : TypeBuilder) (ilGen : ILGenerator) (body : SExp list) =
    match body with
    | [] ->
        let emptyListGetter = typeof<Value>.GetMethod "get_EmptyList"
        ilGen.Emit(OpCodes.Call, emptyListGetter)
        ilGen.Emit(OpCodes.Ret)
    | [last] ->
        generate context typeBuilder ilGen last
        ilGen.Emit(OpCodes.Ret)
    | sexp :: rest ->
        generate context typeBuilder ilGen sexp
        generateBody context typeBuilder ilGen rest
and private pushValue (context : Context) (ilGen : ILGenerator) (value : Value) =
    match value with
    | Number n ->
        let numberCons = typeof<Value>.GetMethod "NewNumber"
        ilGen.Emit(OpCodes.Ldc_R8,n)
        ilGen.Emit(OpCodes.Call,numberCons)
    | Symbol name ->
        try
            let local = context.locals.[name]
            ilGen.Emit(OpCodes.Ldloc,local)
        with
        | :? KeyNotFoundException -> failwithf "Symbol %A not bound." name
    | String s ->
        let stringCons = typeof<Value>.GetMethod "NewString"
        ilGen.Emit(OpCodes.Ldstr,s)
        ilGen.Emit(OpCodes.Call,stringCons)
    | Cons (carval,cdrval) ->
        let consCons = typeof<Value>.GetMethod "NewCons"
        pushValue context ilGen carval
        pushValue context ilGen cdrval
        ilGen.Emit(OpCodes.Call, consCons)
    | EmptyList ->
        let emptyCons = typeof<Value>.GetMethod "get_EmptyList"
        ilGen.Emit(OpCodes.Call,emptyCons)

let compile (source : string) (assemblyName : string) (fileName : string) : unit =
    let assemblyName = new AssemblyName(assemblyName)
    let assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save)
    let moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyBuilder.GetName().Name, fileName)
    let typeBuilder = moduleBuilder.DefineType("Program", TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.BeforeFieldInit)
    let methodBuilder = typeBuilder.DefineMethod("Main", MethodAttributes.Public ||| MethodAttributes.Static, typeof<Void>, [| |])
    
    assemblyBuilder.SetEntryPoint methodBuilder
    
    let ilGenerator = methodBuilder.GetILGenerator()

    let context = Context.create ()
    prologue ilGenerator
    let sexp = Reader.parse source
    generate context typeBuilder ilGenerator sexp

    epilogue context ilGenerator

    typeBuilder.CreateType()
    |> ignore

    assemblyBuilder.Save fileName
