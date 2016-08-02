with import <nixpkgs> {}; rec {
  frameworkVersion = "4.5";
  fsharpVersion = "4.0";
  assemblyVersion = "4.4.0.0";

  naggumEnv = stdenv.mkDerivation {
    name = "naggum";
    buildInputs = [ mono fsharp dotnetPackages.Nuget ];

    # For more info regarding this variable see:
    # https://github.com/NixOS/nixpkgs/blob/d4681bf62672083f92545e02e00b8cf040247e8d/pkgs/build-support/dotnetbuildhelpers/patch-fsharp-targets.sh
    FSharpTargetsPath="${fsharp}/lib/mono/${frameworkVersion}/Microsoft.FSharp.Targets";

    # For more info regarding this variable see:
    # http://www.mono-project.com/docs/advanced/assemblies-and-the-gac/
    MONO_PATH="${fsharp}/lib/mono/Reference Assemblies/Microsoft/FSharp/.NETFramework/v${fsharpVersion}/${assemblyVersion}/";
  };
}
