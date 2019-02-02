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
                var analyzer = new Analyzer(args[0]);
                Symbol token;
                do
                {
                    token = analyzer.getNextToken();
                    Console.WriteLine(token.ToString());
                } while (token.token != Tokens.eoft);
            }

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }
    }
}
