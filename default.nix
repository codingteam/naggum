with import <nixpkgs> {}; rec {
  frameworkVersion = "4.5";
  fsharpVersion = "4.0";
  assemblyVersion = "4.4.0.0";

  naggumEnv = stdenv.mkDerivation {
    name = "naggum";
    buildInputs = [ mono fsharp dotnetPackages.Nuget ];
    FSharpTargetsPath="${fsharp}/lib/mono/${frameworkVersion}/Microsoft.FSharp.Targets";
    MONO_PATH="${fsharp}/lib/mono/Reference Assemblies/Microsoft/FSharp/.NETFramework/v${fsharpVersion}/${assemblyVersion}/";
  };
}
