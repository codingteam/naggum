namespace Naggum.Assembler.Representation

open System.Reflection

type MetadataItem =
    | EntryPoint

type Visibility =
    | Public

type Instruction =
    | Ldstr of string
    | Call of MethodInfo
    | Ret

type MethodDefinition =
    { Metadata : Set<MetadataItem>
      Visibility : Visibility
      Name : string
      ReturnType : System.Type
      Body : Instruction list }

type AssemblyUnit =
    | Method of MethodDefinition

type Assembly =
    { Name : string
      Units : AssemblyUnit list }
