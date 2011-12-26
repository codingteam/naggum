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
module Naggum.Compiler.FormGenerator

open System.Collections.Generic
open System.Reflection
open System.Reflection.Emit
open Naggum.Reader
open Naggum.IGenerator
open Naggum.Compiler.Context
open Naggum.Generator

type FormGenerator() =
    interface IGenerator
        with member this.Generate _ = failwith "Internal compiler error: unreified form generator invoked"

type ValueGenerator(context:Context,value:Value) =
    inherit FormGenerator()
    interface IGenerator
        with member this.Generate _ = failwith "Internal compiler error: unreified value generator invoked"

type SymbolGenerator(context:Context,name:string) =
    inherit ValueGenerator(context,Symbol name)
    interface IGenerator
        with member this.Generate ilGen =
                try
                    let local = context.locals.[name]
                    ilGen.Emit(OpCodes.Ldloc,local)
                with
                | :? KeyNotFoundException -> failwithf "Symbol %A not bound." name

type ConstantGenerator(context:Context,o:obj) =
    inherit ValueGenerator(context,Object o)
    interface IGenerator with
        member this.Generate ilGen =
            let gen = generator o
            gen.Generate ilGen