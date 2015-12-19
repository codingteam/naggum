module Naggum.Compiler.IGenerator

open System
open System.Reflection
open System.Reflection.Emit

open Naggum.Compiler.Reader
open Naggum.Compiler.Context

//TODO: Add a method that returns generated values' types without actually emitting the code.
type IGenerator =
    interface
        abstract ReturnTypes : unit -> Type list
        abstract Generate : ILGenerator -> unit
    end

type IGeneratorFactory =
    interface
        abstract MakeGenerator : Context -> SExp -> IGenerator
        abstract MakeSequence : Context -> SExp list -> IGenerator
        abstract MakeBody : Context -> SExp list -> IGenerator
        abstract MakeGeneratorFactory : TypeBuilder -> IGeneratorFactory
    end