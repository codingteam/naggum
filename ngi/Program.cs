using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Naggum.Runtime;

namespace ngi
{
    class Program
    {
        static void Main(string[] args)
        {
            Stream input = System.Console.OpenStandardInput();
            for (; ; )
            {
                System.Console.Out.Write(">");
                Object obj = Reader.Read(input);
                System.Console.Out.WriteLine(obj.ToString());
            }
            input.Close();
        }
    }
}
