module Naggum.Compiler.Generator

open System
open System.IO
open System.Reflection
open System.Reflection.Emit

open GrEmit

open Naggum.Backend
open Naggum.Backend.Reader
open Naggum.Compiler.IGenerator
open Naggum.Compiler.GeneratorFactory

let compileMethod context (generatorFactory : IGeneratorFactory) body (methodBuilder : MethodBuilder) fileName =
    use il = new GroboIL (methodBuilder)
    try
        let gen = generatorFactory.MakeBody context body
        gen.Generate il
    with
    | ex -> printfn "File: %A\nForm: %A\nError: %A" fileName sexp ex.Source

    il.Ret ()

let compile (source : Stream) (assemblyName : string) (filePath : string) (asmRefs:string list): unit =
    let assemblyName = AssemblyName assemblyName
    let path = Path.GetDirectoryName filePath
    let assemblyPath = if path = "" then null else path
    let fileName = Path.GetFileName filePath
    let appDomain = AppDomain.CurrentDomain

    let assemblyBuilder = appDomain.DefineDynamicAssembly (assemblyName, AssemblyBuilderAccess.Save, assemblyPath)
    Globals.ModuleBuilder <- assemblyBuilder.DefineDynamicModule(assemblyBuilder.GetName().Name, fileName)
    let typeBuilder = Globals.ModuleBuilder.DefineType("Program", TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.BeforeFieldInit)
    let methodBuilder = typeBuilder.DefineMethod ("Main",
                                                  MethodAttributes.Public ||| MethodAttributes.Static,
                                                  typeof<Void>,
                                                  [| |])

    let gf = new GeneratorFactory(typeBuilder, methodBuilder) :> IGeneratorFactory
    assemblyBuilder.SetEntryPoint methodBuilder

    let context = Context.create ()

    //loading language runtime
    let rta = Assembly.LoadFrom("Naggum.Runtime.dll")
    context.loadAssembly rta

    // Load .NET runtime and all referenced assemblies:
    context.loadAssembly <| Assembly.Load "mscorlib"
    List.iter context.loadAssembly (List.map Assembly.LoadFrom asmRefs)

    let body = Reader.parse fileName source
    compileMethod context gf body methodBuilder fileName

    typeBuilder.CreateType()
    |> ignore

    assemblyBuilder.Save fileName
