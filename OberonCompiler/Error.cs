using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    static class Error
    {
        public static void Crash(string format, params object[] args)
        {
            Console.WriteLine(format, args);
            Console.ReadLine();
            Environment.Exit(1);
        }
    }
}
