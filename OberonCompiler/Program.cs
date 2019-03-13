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
                Record rec = null;
                var analyzer = new Analyzer(args[0]);
                var symTable = new SymTable();
                Symbol token;
                int depth = 0;
                while((token = analyzer.getNextToken()).token != Tokens.eoft)
                {

                    symTable.Insert(token.lexeme, token, depth);
                    if (token.token == Tokens.proceduret)
                    {
                        depth++;
                        rec = symTable.Lookup(token.lexeme);
                    }

                }
                symTable.WriteTable(0);
            }

            Console.ReadLine();
        }
    }
}
