module Naggum.Runtime

open System
open Types
open Context

let add = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        eprintfn "Expected: Number\nGot: %A" any
                                        raise (new ArgumentException()))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        eprintfn "Expected: Number\nGot: Quote %A" q
                                        raise (new ArgumentException()))
            List.reduce (+) args |> Number |> Atom

let sub = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        eprintfn "Expected: Number\nGot: %A" any
                                        raise (new ArgumentException()))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        eprintfn "Expected: Number\nGot: Quote %A" q
                                        raise (new ArgumentException()))
            List.reduce (-) args |> Number |> Atom

let mul = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        eprintfn "Expected: Number\nGot: %A" any
                                        raise (new ArgumentException()))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        eprintfn "Expected: Number\nGot: Quote %A" q
                                        raise (new ArgumentException()))
            List.reduce (*) args |> Number |> Atom

let div = fun (sexp) ->
            let args = List.map (fun (x) ->
                                    match x with
                                    |Atom (Number n) -> n
                                    |List ([Atom (Number n)]) -> n
                                    |any ->
                                        eprintfn "Expected: Number\nGot: %A" any
                                        raise (new ArgumentException()))
                                (match sexp with
                                    | List xs -> xs
                                    | Atom a -> [Atom a]
                                    | Quote q -> 
                                        eprintfn "Expected: Number\nGot: Quote %A" q
                                        raise (new ArgumentException()))
            List.reduce (/) args |> Number |> Atom

let equal = fun (sexp) ->
            let args =
                (match sexp with
                 | List xs -> xs
                 | Atom a -> [Atom a]
                 | Quote q -> [Quote q])
            if (List.forall (fun (elem) -> elem = args.[0]) args) then
                Atom (Symbol "t")
            else
                List []

let load (ctx:Context) =
    ctx.add (Symbol "add") (Function add)
    ctx.add (Symbol "sub") (Function sub)
    ctx.add (Symbol "mul") (Function mul)
    ctx.add (Symbol "div") (Function div)
    ctx.add (Symbol "equal") (Function equal)