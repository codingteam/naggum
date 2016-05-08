module Naggum.Compiler.GeneratorFactory

open System
open System.Reflection.Emit
open System.Text.RegularExpressions

open Naggum.Backend
open Naggum.Backend.MaybeMonad
open Naggum.Backend.Reader
open Naggum.Backend.Matchers
open Naggum.Compiler.ClrGenerator
open Naggum.Compiler.Context
open Naggum.Compiler.FormGenerator
open Naggum.Compiler.IGenerator
open Naggum.Compiler.MathGenerator
open Naggum.Compiler.NumberGen
open Naggum.Compiler.StringGen
open Naggum.Runtime

type GeneratorFactory(typeBuilder : TypeBuilder,
                      methodBuilder : MethodBuilder) =
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
        | Reader.Symbol name ->
            (new SymbolGenerator(context,name)) :> IGenerator
        | Reader.Object o -> this.makeObjectGenerator o

    member private this.MakeFormGenerator (context : Context, form : SExp list) : IGenerator =
        match form with
        | Symbol "defun" :: Symbol name :: List args :: body ->
            upcast new DefunGenerator (context, typeBuilder, name, args, body, this)
        | [Symbol "if"; condition; ifTrue; ifFalse] -> // full if form
            upcast new FullIfGenerator (context, condition, ifTrue, ifFalse, this)
        | [Symbol "if"; condition; ifTrue] -> // reduced if form
            upcast new ReducedIfGenerator (context,  condition, ifTrue, this)
        | Symbol "let" :: bindings :: body -> // let form
            upcast new LetGenerator (context,
                                     typeof<Void>,
                                     bindings,
                                     body,
                                     this)
        | [Symbol "quote"; quotedExp] ->
            upcast new QuoteGenerator (context, quotedExp, this)
        | Symbol "new" :: Symbol typeName :: args ->
            upcast new NewObjGenerator(context,  typeName, args, this)
        | Symbol "+" :: args ->
            upcast new ArithmeticGenerator (context, args, Add, this)
        | Symbol "-" :: args ->
            upcast new ArithmeticGenerator (context, args, Sub, this)
        | Symbol "*" :: args ->
            upcast new ArithmeticGenerator (context, args, Mul, this)
        | Symbol "/" :: args ->
            upcast new ArithmeticGenerator (context, args, Div, this)
        | Symbol "=" :: argA :: argB :: [] ->
            upcast new SimpleLogicGenerator (context, argA, argB, Ceq, this)
        | Symbol "<" :: argA :: argB :: [] ->
            upcast new SimpleLogicGenerator (context, argA, argB, Clt, this)
        | Symbol ">" :: argA :: argB :: [] ->
            upcast new SimpleLogicGenerator (context, argA, argB, Cgt, this)
        | Symbol "call" :: Symbol fname :: instance :: args ->
            upcast new InstanceCallGenerator (context, instance, fname, args, this)
        | Symbol fname :: args -> // generic funcall pattern
            let tryGetType typeName =
                try Some (context.types.[Symbol(typeName)]) with
                | _ ->
                    try Some (Type.GetType typeName) with
                    | _ -> None

            let callRegex = Regex(@"([\w\.]+)\.(\w+)", RegexOptions.Compiled)
            let callMatch = callRegex.Match fname
            let maybeClrType =
                maybe {
                    let! typeName = if callMatch.Success then Some callMatch.Groups.[1].Value else None
                    let! clrType = tryGetType typeName
                    return clrType
                }

            if Option.isSome maybeClrType then
                let clrType = Option.get maybeClrType
                let methodName = callMatch.Groups.[2].Value
                upcast new ClrCallGenerator (context, clrType, methodName, args, this)
            else
                upcast new FunCallGenerator(context, fname, args, this)
        | _ -> failwithf "Form %A is not supported yet" list

    member private this.MakeSequenceGenerator (context: Context, seq : SExp list) =
        new SequenceGenerator (context, seq, (this :> IGeneratorFactory))

    member private this.MakeBodyGenerator (context : Context, body : SExp list) =
        new BodyGenerator (context, methodBuilder.ReturnType, body, this)

    interface IGeneratorFactory with
        member this.MakeGenerator context sexp =
            match sexp with
            | Atom value -> this.makeValueGenerator (context, value)
            | List form -> this.MakeFormGenerator (context,form)

        member this.MakeSequence context seq = this.MakeSequenceGenerator (context,seq) :> IGenerator

        member this.MakeBody context body = this.MakeBodyGenerator (context,body) :> IGenerator

        member this.MakeGeneratorFactory newTypeBuilder newMethodBuilder =
            new GeneratorFactory(newTypeBuilder,
                                 newMethodBuilder) :> IGeneratorFactory
