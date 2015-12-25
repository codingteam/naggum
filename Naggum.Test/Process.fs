module Naggum.Test.Process

open System.Diagnostics

let run fileName =
    // TODO: Mono check
    let startInfo = ProcessStartInfo (fileName,
                                      UseShellExecute = false,
                                      RedirectStandardOutput = true)
    use p = Process.Start startInfo
    p.WaitForExit ()
    p.StandardOutput.ReadToEnd().Replace ("\r\n", "\n")
