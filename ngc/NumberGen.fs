(*  Copyright (C) 2011-2013 by ForNeVeR, Hagane

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
module Naggum.Compiler.NumberGen

open IGenerator
open System
open System.Reflection
open System.Reflection.Emit

type NumberGen<'TNumber>() =
    interface IGenerator with
        member this.Generate _ = failwith "Failure: Tried to generate unreified number constant.\n"
        member this.ReturnTypes () = [typeof<'TNumber>]        

type Int32Gen(number: Int32) =
    inherit NumberGen<Int32>()
    interface IGenerator with
        member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_I4, number)

type Int64Gen(number: Int64) =
    inherit NumberGen<Int64>()
    interface IGenerator with
        member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_I8, number)

type SingleGen(number: Single) =
    inherit NumberGen<Single>()
    interface IGenerator with
        member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_R4, number)

type DoubleGen(number: Double) =
    inherit NumberGen<Double>()
    interface IGenerator with
        member this.Generate ilGen = ilGen.Emit(OpCodes.Ldc_R8, number)
