module Naggum.Compiler.NumberGen

open IGenerator
open System
open System.Reflection
open System.Reflection.Emit

type NumberGen<'TNumber>(number: 'TNumber) =
    interface IGenerator with
        member this.Generate _ = failwith "Failure: Tried to generate unreified number constant.\n"
        member this.ReturnTypes () = [typeof<'TNumber>]        

type Int32Gen(number: Int32) =
    inherit NumberGen<Int32>(number)
    interface IGenerator with
        member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_I4, number)

type Int64Gen(number: Int64) =
    inherit NumberGen<Int64>(number)
    interface IGenerator with
        member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_I8, number)

type SingleGen(number: Single) =
    inherit NumberGen<Single>(number)
    interface IGenerator with
        member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_R4, number)

type DoubleGen(number: Double) =
    inherit NumberGen<Double>(number)
    interface IGenerator with
        member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_R8, number)
