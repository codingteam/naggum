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

module Naggum.Compiler.Generator

open System
open System.Reflection
open System.Reflection.Emit

open Naggum.Context
open Naggum.Reader
open Naggum.Runtime
open Naggum.Types

/// Returns local variable for Context object.
let private prologue (ilGen : ILGenerator) =
    let listGetter = typeof<List<string * ContextItem>>.GetMethod "get_Empty"
    let contextConstructor = typeof<Context>.GetConstructor [| typeof<List<string * ContextItem>> |]
    let runtimeLoad = typeof<Runtime>.GetMethod "load"

    ilGen.BeginScope()
    let contextVar = ilGen.DeclareLocal typeof<Context>

    ilGen.Emit(OpCodes.Call, listGetter)
    ilGen.Emit(OpCodes.Newobj, contextConstructor)
    ilGen.Emit(OpCodes.Stloc, contextVar)
    ilGen.Emit(OpCodes.Ldloc, contextVar.LocalIndex)
    ilGen.Emit(OpCodes.Call, runtimeLoad)

    contextVar

let private genApply (contextVar : LocalBuilder) (funcName : string) (ilGen : ILGenerator) : unit =
    let newSymbol = typeof<Value>.GetMethod "NewSymbol"
    let contextGet = typeof<Context>.GetMethod "get"
    let valueGetter = typeof<ContextItem option>.GetMethod "get_Value"
    let functionType = typeof<ContextItem>.GetNestedType "Function"
    let functionItemGetter = functionType.GetMethod "get_Item"
    let funcInvoke = typeof<SExp -> SExp>.GetMethod "Invoke"

    ilGen.Emit(OpCodes.Ldloc, contextVar.LocalIndex)
    ilGen.Emit(OpCodes.Ldstr, funcName)
    ilGen.Emit(OpCodes.Call, newSymbol)
    ilGen.Emit(OpCodes.Call, contextGet)
    ilGen.Emit(OpCodes.Call, valueGetter)
    ilGen.Emit(OpCodes.Castclass, functionType)
    ilGen.Emit(OpCodes.Call, functionItemGetter)
    ilGen.Emit(OpCodes.Call, funcInvoke)

let private epilogue contextVar (ilGen : ILGenerator) =
    let argGetter = typeof<Value>.GetMethod "get_EmptyList"

    ilGen.Emit(OpCodes.Call, argGetter)
    genApply contextVar "main" ilGen

    ilGen.Emit OpCodes.Ret
    ilGen.EndScope()

let rec private generate (typeBuilder : TypeBuilder) (ilGen : ILGenerator) (form : SExp) (contextVar : LocalBuilder) =
    match form with
    | List list ->
        match list with
        | (Atom (Symbol "defun") :: Atom (Symbol name) :: List args :: body) ->
            let argsDef = Array.create (List.length args) typeof<obj>
            let methodGen = typeBuilder.DefineMethod(name, MethodAttributes.Public ||| MethodAttributes.Static, typeof<obj>, argsDef)
            generateBody typeBuilder ilGen body contextVar

            // Add function to context:
            let newSymbol = typeof<Value>.GetMethod "NewSymbol"
            let converterConstructor = typeof<Converter<SExp, SExp>>.GetConstructor [| typeof<obj>; typeof<nativeint> |]
            let fSharpFuncFromConverter = typeof<SExp -> SExp>.GetMethod "FromConverter"
            let newFunction = typeof<ContextItem>.GetMethod "NewFunction"
            let contextAdd = typeof<Context>.GetMethod "add"

            ilGen.Emit(OpCodes.Ldloc, contextVar.LocalIndex) // Context

            ilGen.Emit(OpCodes.Ldstr, name)
            ilGen.Emit(OpCodes.Call, newSymbol) // Context, Symbol

            // Generate Converter instance:
            ilGen.Emit(OpCodes.Ldnull) // Context, Symbol, null
            ilGen.Emit(OpCodes.Ldftn, methodGen) // Context, Symbol, null, method
            ilGen.Emit(OpCodes.Newobj, converterConstructor) // Context, Symbol, Converter

            // Generate FSharpFunc:            
            ilGen.Emit(OpCodes.Call, fSharpFuncFromConverter)
            ilGen.Emit(OpCodes.Call, newFunction) // Context, Symbol, Function

            ilGen.Emit(OpCodes.Call, contextAdd)
        | Atom (Symbol "if") :: condition :: if_true :: if_false :: [] ->
            generate typeBuilder ilGen condition contextVar
            let if_true_lbl = ilGen.DefineLabel()
            let end_form = ilGen.DefineLabel()
            ilGen.Emit (OpCodes.Brtrue, if_true_lbl)
            generate typeBuilder ilGen if_false contextVar
            ilGen.Emit (OpCodes.Br,end_form)
            ilGen.MarkLabel if_true_lbl
            generate typeBuilder ilGen if_true contextVar
            ilGen.MarkLabel end_form
        | _ -> failwithf "%A not supported yet." list
    | other     -> failwithf "%A form not supported yet." other
and private generateBody (typeBuilder : TypeBuilder) (ilGen : ILGenerator) (body : SExp list) (contextVar : LocalBuilder) =
    match body with
    | [] ->
        let emptyListGetter = typeof<Value>.GetMethod "get_EmptyList"
        ilGen.Emit(OpCodes.Call, emptyListGetter)
        ilGen.Emit(OpCodes.Ret)
    | [last] ->
        generate typeBuilder ilGen last contextVar
        ilGen.Emit(OpCodes.Ret)
    | sexp :: rest ->
        generate typeBuilder ilGen sexp contextVar
        generateBody typeBuilder ilGen rest contextVar  

let compile (source : string) (assemblyName : string) (fileName : string) : unit =
    let assemblyName = new AssemblyName(assemblyName)
    let assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save)
    let moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyBuilder.GetName().Name, fileName)
    let typeBuilder = moduleBuilder.DefineType("Program", TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.BeforeFieldInit)
    let methodBuilder = typeBuilder.DefineMethod("Main", MethodAttributes.Public ||| MethodAttributes.Static, typeof<Void>, [| |])
    
    assemblyBuilder.SetEntryPoint methodBuilder
    
    let ilGenerator = methodBuilder.GetILGenerator()

    let contextVar = prologue ilGenerator
    let sexp = Reader.parse source
    generate typeBuilder ilGenerator sexp contextVar

    epilogue contextVar ilGenerator

    typeBuilder.CreateType()
    |> ignore

    assemblyBuilder.Save fileName
