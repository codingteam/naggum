Naggum Build Guide
==================

Windows
-------

To build Naggum under Windows, just use Visual Studio or MSBuild like that:

    $ cd naggum
    $ nuget restore
    $ msbuild /p:Configuration=Release Naggum.sln

Linux
-----

See general build instructions for Linux in the file `.travis.yml` inside of the
Naggum root source directory.

You'll need [Mono][mono], [NuGet][nuget] and [F# Compiler][fsharp] installed in
you current environment. Some of them may or may not be part of your Mono
installation; just make sure you have them all properly installed.

Below is an example of setting up these tools on [NixOS Linux][nixos]; feel free
to add instructions for any other non-standard distributives.

### NixOS Linux

First of all, install `mono` and `fsharp` packages to your environment:

    $ nix-env -i mono fsharp

According to the recommendations in
[`patch-fsharp-targets`][patch-fsharp-targets] build helpers, you may want to
set up the environment variable `FSharpTargetsPath` either globally or locally:

    $ export FSharpTargetsPath=$(dirname $(which fsharpc))/../lib/mono/4.5/Microsoft.FSharp.Targets

You'll need a NuGet executable. Unfortunately, there is no NixOS package for
NuGet, so you need to download it manually:

    $ cd naggum
    $ wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe

After that you can download the dependencies and build the project using
`xbuild`:

    $ mono nuget.exe restore Naggum.sln
    $ xbuild /p:Configuration=Release /p:TargetFrameworkVersion="v4.5" Naggum.sln

[fsharp]: http://fsharp.org/
[mono]: http://www.mono-project.com/
[nixos]: http://nixos.org/
[nuget]: http://www.nuget.org/
[patch-fsharp-targets]: https://github.com/NixOS/nixpkgs/blob/d4681bf62672083f92545e02e00b8cf040247e8d/pkgs/build-support/dotnetbuildhelpers/patch-fsharp-targets.sh
