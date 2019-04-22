using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine(String.Format("Usage: {0} filename",
                    System.AppDomain.CurrentDomain.FriendlyName));
            }
            else
            {
                SymTable symTable = new SymTable();
                string TACPath = args[0].TrimEnd(".obr".ToArray()) + ".tac";
                using (StreamWriter sw = File.CreateText(TACPath))
                {
                    RDParser parser = new RDParser();
                    Analyzer a = new Analyzer(args[0]);
                    Emitter e = new Emitter(symTable, sw);
                    parser.Goal(a, symTable, e);
                }

                using (StreamReader sr = File.OpenText(TACPath))
                {
                    string asmPath = args[0].TrimEnd(".obr".ToArray()) + ".asm";
                    using (StreamWriter sw = File.CreateText(asmPath))
                    {
                        CodeGenerator gen = new CodeGenerator(symTable, sr, sw);
                        gen.Generate();
                    }
                }
            }

            Console.ReadLine();
        }
    }
}
