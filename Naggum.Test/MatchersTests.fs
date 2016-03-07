module Naggum.Test.MatchersTests

open Xunit

open Naggum.Backend
open Naggum.Backend.Matchers

[<Fact>]
let ``String should be matched by Object matcher`` () =
    let string = Reader.Atom (Reader.Object "xxx")
    let isObject =
        match string with
        | Object _ -> true
        | _ -> false

    Assert.True isObject
