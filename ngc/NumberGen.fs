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
module Naggum.Compiler.NumberGen

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
        interface IGenerator with
            member this.Generate ilGen =
                ilGen.Emit(OpCodes.Ldc_I4,number)
                [typeof<Int32>]
    end

type Int64Gen(number: Int64) =
    class
        inherit NumberGen()
        interface IGenerator with
            member this.Generate ilGen =
                ilGen.Emit(OpCodes.Ldc_I8,number)
                [typeof<Int64>]
    end

type SingleGen(number: Single) =
    class
        inherit NumberGen()
        interface IGenerator with
            member this.Generate ilGen =
                ilGen.Emit(OpCodes.Ldc_R4,number)
                [typeof<Single>]
    end

type DoubleGen(number: Double) =
    class
        inherit NumberGen()
        interface IGenerator with
            member this.Generate ilGen =
                ilGen.Emit(OpCodes.Ldc_R8,number)
                [typeof<Single>]
    end