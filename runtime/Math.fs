(*  Copyright (C) 2011 by Hagane, ForNeVeR

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
module Naggum.Runtime

open System
open Reader

type Math =
    static member public add (args: obj list) =
        List.reduce (+) (List.map unbox args) :> obj

    static member public sub (args: obj list) =
        List.reduce (-) (List.map unbox args) :> obj

    static member public mul (args: obj list) =
        List.reduce (*) (List.map unbox args) :> obj

    static member public div (args: obj list) =
        List.reduce (/) (List.map unbox args) :> obj

    static member public equal(args: obj list) =
        match args.Length with
        | 0 -> (false :> obj)
        | 1 -> (true :> obj)
        | any -> (List.forall (fun (a) -> a.Equals (args.Item 0)) args :> obj)