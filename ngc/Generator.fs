module Naggum.Compiler.Generator

open System
open System.IO
open System.Reflection
open System.Reflection.Emit

open Naggum.Compiler.IGenerator
open Naggum.Compiler.GeneratorFactory
open Naggum.Compiler.Reader

let private prologue (ilGen : ILGenerator) =
    ilGen.BeginScope()

let private epilogue context (ilGen : ILGenerator) =
    ilGen.Emit OpCodes.Ret
    ilGen.EndScope()

let compileMethod context (generatorFactory : IGeneratorFactory) body (methodBuilder : MethodBuilder) fileName =
    let ilGenerator = methodBuilder.GetILGenerator()
    
    prologue ilGenerator
    try
        let gen = generatorFactory.MakeBody context body
        gen.Generate ilGenerator
    with
    | ex -> printfn "File: %A\nForm: %A\nError: %A" fileName sexp ex.Source

    epilogue context ilGenerator

let compile (source : Stream) (assemblyName : string) (fileName : string) (asmRefs:string list): unit =
    let assemblyName = new AssemblyName(assemblyName)
    let assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save)
    Globals.ModuleBuilder <- assemblyBuilder.DefineDynamicModule(assemblyBuilder.GetName().Name, fileName)
    let typeBuilder = Globals.ModuleBuilder.DefineType("Program", TypeAttributes.Public ||| TypeAttributes.Class ||| TypeAttributes.BeforeFieldInit)
    let methodBuilder = typeBuilder.DefineMethod ("Main",
                                                  MethodAttributes.Public ||| MethodAttributes.Static,
                                                  typeof<Void>,
                                                  Array.Empty ())
    
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
