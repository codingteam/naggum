module Naggum.Backend.MaybeMonad

type MaybeMonad() =

    member __.Bind (m, f) =
        match m with
        | Some v -> f v
        | None -> None

    member __.Return v = Some v

let maybe = MaybeMonad ()
