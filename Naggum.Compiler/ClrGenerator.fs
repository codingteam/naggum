﻿module Naggum.Compiler.ClrGenerator

open System

open Naggum.Backend.MaybeMonad
open Naggum.Backend.Reader
open Naggum.Compiler.Context
open Naggum.Compiler.IGenerator

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

type ClrCallGenerator (context : Context,
                       clrType : Type,
                       methodName : string,
                       arguments : SExp list,
                       gf : IGeneratorFactory) =
    let args = gf.MakeSequence context arguments
    let arg_types = args.ReturnTypes()
    let clrMethod = nearestOverload clrType methodName arg_types
    interface IGenerator with
        member __.Generate il =
            args.Generate il
            il.Call (Option.get clrMethod)

        member this.ReturnTypes() =
            [(Option.get clrMethod).ReturnType]

type InstanceCallGenerator (context : Context,
                            instance : SExp,
                            methodName : string,
                            arguments : SExp list,
                            gf : IGeneratorFactory) =
    interface IGenerator with
        member __.Generate il =
            let instGen = gf.MakeGenerator context instance
            let argsGen = gf.MakeSequence context arguments
            let methodInfo = nearestOverload (instGen.ReturnTypes() |> List.head) methodName (argsGen.ReturnTypes())
            if Option.isSome methodInfo then
                instGen.Generate il
                argsGen.Generate il
                il.Call (Option.get methodInfo)
            else failwithf "No overload found for method %A with types %A" methodName (argsGen.ReturnTypes())

        member this.ReturnTypes () =
            let inst_gen = gf.MakeGenerator context instance
            let args_gen = gf.MakeSequence context arguments
            let methodInfo = nearestOverload (inst_gen.ReturnTypes() |> List.head) methodName (args_gen.ReturnTypes())
            if Option.isSome methodInfo then
                [(Option.get methodInfo).ReturnType]
            else failwithf "No overload found for method %A with types %A" methodName (args_gen.ReturnTypes())
