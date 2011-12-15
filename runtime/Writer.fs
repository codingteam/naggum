module Naggum.Writer

type Writer =
    static member write (o : obj) =
        let str = o.ToString()
        System.Console.Write str

    static member writeln (o : obj) =
        let str = o.ToString()
        System.Console.WriteLine str