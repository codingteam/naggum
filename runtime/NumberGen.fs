module Naggum.NumberGen

open IGenerator
open System
open System.Reflection
open System.Reflection.Emit

type NumberGen() =
    interface IGenerator
        with member this.Generate _ = failwith "Failure: Tried to generate unreified number constant.\n"

type Int32Gen(number: Int32) =
    class
        inherit NumberGen()
        interface IGenerator
            with member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_I4,number)
    end

type Int64Gen(number: Int64) =
    class
        inherit NumberGen()
        interface IGenerator
            with member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_I8,number)
    end

type SingleGen(number: Single) =
    class
        inherit NumberGen()
        interface IGenerator
            with member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_R4,number)
    end

type DoubleGen(number: Double) =
    class
        inherit NumberGen()
        interface IGenerator
            with member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_R8,number)
    end