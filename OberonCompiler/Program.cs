using System;
using System.Collections.Generic;
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
                RDParser parser = new RDParser();
                Analyzer a = new Analyzer(args[0]);

                parser.Goal(a, symTable);
            }

            Console.ReadLine();
        }
    }
}
