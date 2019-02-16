using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    class Parser
    {
        const string schemaPath = "parserSchema.txt";
        ParserTable parserTable = new ParserTable();
        Analyzer analyzer;
        Symbol currentToken;
        Symbol nextToken;

        public bool Goal(Analyzer analyzer)
        {
            this.analyzer = analyzer;
            currentToken = analyzer.getNextToken();
            nextToken = analyzer.getNextToken();
            if (!buildParserTableFromSchema())
                return false;

            processGrammarRule("Start");

            return true;
        }

        protected void processGrammarRule(string variable)
        {
            
        }

        protected bool buildParserTableFromSchema()
        {
            string[] lines;
            bool hasError = false;
            try
            {
                lines = File.ReadAllLines(schemaPath);
            }
            catch
            {
                Console.WriteLine("Error in parser: Error opening schema file. Please check that it exists and have read permissions");
                return false;
            }

            foreach( var item in lines.Select((value, i) => new { i, value }))
            {
                var lineNumber = item.i;
                var line = item.value;

                try
                {
                    var words = line.Split(' ').Where((string i) =>
                    {
                        return !String.IsNullOrWhiteSpace(i);
                    }).ToList();
                    var productionVariable = words[0];
                    List<string> productionSentence;

                    if (words[1] != "->")
                        throw new Exception("'->' Operator missing or misplaced");
                    if (words.Count - 2 <= 0)
                        throw new Exception("Incomplete or malformed grammar");

                    productionSentence = words.Skip(2).ToList();

                    //Verify grammar is not left-recursive
                    if (words[0] == productionSentence[0])
                        throw new Exception(string.Format("Schema error in production '{0}' - left recursive productions are FORBIDDEN", line));

                    //verify production
                    foreach( string token in productionSentence)
                    {
                        // Verify tokens are valid
                        if (!Char.IsUpper(token[0]))
                        {
                            if (MapStringToToken(token) == Tokens.unknownt)
                            {
                                throw new Exception(String.Format("Unknown token '{0}' in production '{1}'", token,
                                    line));
                            }
                        }
                    }

                    //Add production to dictionary
                    parserTable.Add(words[0],productionSentence);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in parser: Schema error on line {0} - Invalid Syntax - {1}", lineNumber.ToString(), e.Message);
                    return false;
                }
            }

            //Verify dictionary
            if (!parserTable.grammarRules.ContainsKey("Start"))
            {
                Console.WriteLine("Error in parser: Schema does not contian 'Start' variable. Please revise schema.");
            }

            //Verify all referenced variables exist
            foreach (var item in parserTable.grammarRules)
            {
                var productionSentences = item.Value.productions;
                foreach (var sentence in productionSentences)
                {
                    foreach (var token in sentence)
                    {
                        //Variables will have first character uppercase
                        if (Char.IsUpper(token[0]))
                        {
                            if (!parserTable.grammarRules.ContainsKey(token))
                            {
                                Console.WriteLine("Error in parser: Schema error in production '{0}' - Unknown Variable reference '{1}'", token, sentence);
                                hasError = true;
                            }
                        }
                    }
                }
            }

            //Return true if successful, false if we had an error
            return !hasError;
        }

        protected Tokens MapStringToToken(string value)
        {
            try { return (Tokens)Enum.Parse(typeof(Tokens), value); }
            catch
            {
                return Tokens.unknownt;
            }
            
        }
    }

    class ParserTable
    {
        public Dictionary<string, Productions> grammarRules = new Dictionary<string, Productions>();

        public void Add(string variable, List<string> production)
        {
            if (!grammarRules.ContainsKey(variable))
                grammarRules.Add(variable, new Productions());
            grammarRules[variable].Add(production);
        }
    }

    class Productions
    {
        public List<List<string>> productions;

        public void Add(List<string> production)
        {
            productions.Add(production);
        }
    }
}
