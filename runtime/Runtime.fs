module Naggum.Runtime

open System
open Types
open Context

let add = fun (args: obj list) ->
            List.reduce (+) (List.map unbox args) :> obj

let sub = fun (args: obj list) ->
            List.reduce (-) (List.map unbox args) :> obj

let mul = fun (args: obj list) ->
            List.reduce (*) (List.map unbox args) :> obj

let div = fun (args: obj list) ->
            List.reduce (/) (List.map unbox args) :> obj

let rec equal = fun (args: obj list) ->
                match args.Length with
                | 0 -> (false :> obj)
                | 1 -> (true :> obj)
                | any -> (List.forall (fun (a) -> a.Equals (args.Item 0)) args :> obj)

let load (ctx:Context) =
    ctx.add (Symbol "add") (Function add)
    ctx.add (Symbol "sub") (Function sub)
    ctx.add (Symbol "mul") (Function mul)
    ctx.add (Symbol "div") (Function div)
    ctx.add (Symbol "equal") (Function equal)