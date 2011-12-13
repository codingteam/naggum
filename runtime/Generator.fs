module Generator

open IGenerator
open NumberGen
open StringGen
open System
open System.Reflection
open System.Reflection.Emit

let generator(o : obj) =
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