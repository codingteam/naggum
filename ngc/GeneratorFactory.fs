﻿(*  Copyright (C) 2011 by ForNeVeR, Hagane

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
module Naggum.Compiler.GeneratorFactory

open IGenerator
open NumberGen
open StringGen
open FormGenerator
open Context
open Naggum.Reader
open System
open System.Reflection
open System.Reflection.Emit

type GeneratorFactory(typeBldr:TypeBuilder) =
    member private this.makeObjectGenerator(o:obj) =
        match o with
        | :? System.Int32 ->
            (new Int32Gen(o :?> System.Int32)) :> IGenerator
        | :? System.Int64 ->
            (new Int64Gen(o :?> System.Int64)) :> IGenerator
        | :? System.Single ->
            (new SingleGen(o :?> System.Single)) :> IGenerator
        | :? System.Double ->
            (new DoubleGen(o :?> System.Double)) :> IGenerator
        | :? System.String ->
            (new StringGen(o :?> System.String)) :> IGenerator
        | other -> failwithf "Not a basic value: %A\n" other

    member private this.makeValueGenerator (context: Context, value:Value) =
        match value with
        | Symbol name ->
            (new SymbolGenerator(context,name)) :> IGenerator
        | Object o -> this.makeObjectGenerator o

    member private this.makeSequenceGenerator(context: Context,seq:SExp list) =
        new SequenceGenerator(context,typeBldr,seq,(this :> IGeneratorFactory))

    member private this.makeBodyGenerator(context: Context,body:SExp list) =
        new BodyGenerator(context,typeBldr,body,(this :> IGeneratorFactory))

    interface IGeneratorFactory with
        member this.MakeGenerator context sexp =
            match sexp with
            | Atom value -> this.makeValueGenerator (context, value)

        member this.MakeSequence context seq = this.makeSequenceGenerator (context,seq) :> IGenerator

        member this.MakeBody context body = this.makeBodyGenerator (context,body) :> IGenerator
