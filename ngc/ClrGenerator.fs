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
module ClrGenerator

open System
open System.Reflection
open System.Reflection.Emit
open Naggum.Runtime
open Naggum.Compiler.Reader
open Naggum.Compiler.Context
open Naggum.Compiler.IGenerator
open Naggum.MaybeMonad

type ClrCallGenerator(context : Context, typeBuilder : TypeBuilder, clrType : Type, methodName : string, arguments : SExp list,
                      gf : IGeneratorFactory) =
    let nearestOverload (clrType : Type) methodName types =
        let rec distanceBetweenTypes (derivedType : Type, baseType) =
            match derivedType with
            | null                     -> None
            | someType
              when someType = baseType -> Some 0
            | _                        ->
                maybe {
                    let! distance = distanceBetweenTypes (derivedType.BaseType, baseType)
                    return distance + 1
                }
        let distance (availableTypes : Type list) (methodTypes : Type list) =
            if availableTypes.Length <> methodTypes.Length then
                None
            else
                Seq.zip availableTypes methodTypes
                |> Seq.map distanceBetweenTypes
                |> Seq.fold (fun state option ->
                                maybe {
                                    let! stateNum = state
                                    let! optionNum = option
                                    return stateNum + optionNum
                                }) (Some 0)
        let methods = clrType.GetMethods() |> Seq.filter (fun clrMethod -> clrMethod.Name = methodName)
        let methodsAndDistances = methods
                                  |> Seq.map (fun clrMethod -> clrMethod,
                                                               distance types (clrMethod.GetParameters()
                                                                               |> Array.map (fun parameter ->
                                                                                             parameter.ParameterType)
                                                                               |> Array.toList))
                                  |> Seq.filter (snd >> Option.isSome)
                                  |> Seq.map (fun (clrMethod, distance) -> clrMethod, Option.get distance)
                                  |> Seq.toList
        if methodsAndDistances.IsEmpty then
            None
        else
            let minDistance = methodsAndDistances |> List.minBy snd |> snd
            let methods = methodsAndDistances |> List.filter (snd >> (fun d -> d = minDistance))
                                              |> List.map fst
            if methods.IsEmpty then
                None
            else
                Some (List.head methods)
    
    interface IGenerator with
        member this.Generate ilGen =
            let args_seq = gf.MakeSequence context arguments
            let arg_types = args_seq.ReturnTypes()
            let clrMethod = nearestOverload clrType methodName arg_types
            let args_seq = gf.MakeSequence context arguments
            let arg_types = args_seq.ReturnTypes()
            args_seq.Generate ilGen            
            ilGen.Emit(OpCodes.Call, Option.get clrMethod)
            if not ((Option.get clrMethod).ReturnType = typeof<System.Void>) then
                ilGen.Emit(OpCodes.Ldnull)
        member this.ReturnTypes() =
            let argTypes = arguments
                           |> List.map (fun sexp -> match sexp with
                                                    | Atom (Object arg) -> arg.GetType()
                                                    | Atom (Symbol _)   -> typeof<obj>
                                                    | List _            -> typeof<obj>
                                                    | any               -> failwithf "Cannot use %A in CLR call." any)
            let clrMethod = nearestOverload clrType methodName argTypes
            [(Option.get clrMethod).ReturnType]
