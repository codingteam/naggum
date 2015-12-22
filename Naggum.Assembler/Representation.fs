namespace Naggum.Assembler.Representation

open System.Reflection

type MetadataItem =
    | EntryPoint

type Visibility =
    | Public

type Type = System.Type

type MethodSignature =
    { Assembly : Assembly option
      ContainingType : Type option
      Name : string
      ArgumentTypes : Type list
      ReturnType : Type }

type Instruction =
    | Call of MethodSignature
    | Ldstr of string
    | Ret

type MethodDefinition =
    { Metadata : Set<MetadataItem>
      Visibility : Visibility
      Name : string
      ArgumentTypes : Type list
      ReturnType : Type
      Body : Instruction list }

type AssemblyUnit =
    | Method of MethodDefinition

type Assembly =
    { Name : string
      Units : AssemblyUnit list }
    override this.ToString () = sprintf "%A" this
