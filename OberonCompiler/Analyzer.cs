using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    enum Tokens {
        modulet, proceduret, vart, begint, endt, ift, thent, elset, elseift, whilet, dot, arrayt,
        recordt, constt, typet, idt, addopt, mulopt, numt, relopt, assignopt, symbolt, unknownt, eoft,
        stringt, errort, emptyt, commat, semicolont, colont, periodt, equalt, lparent, rparent, integert,
        realt, chart
    }

    enum CharTypes { alpha, numerical, period, relational, math, unknown, eof, quote,
        whitespace
    }

    class Analyzer
    {
        protected delegate bool Matches(char x);

        private string file;
        private int filePointer = 0;
        private char curChar = ' ';
        private char nextChar = ' ';
        private int lineNumber = 1;
        private bool eof = false;

        public Analyzer(String fileName)
        {
            file = File.ReadAllText(fileName);            
            fetchChars();
        }

        protected void fetchChars(int howMany = 1)
        {
            for (; howMany > 0; howMany--)
            {
                if (filePointer >= file.Length)
                {
                    curChar = nextChar;
                    nextChar = '\n';
                    if (filePointer > file.Length)
                        eof = true;
                }
                else
                {
                    curChar = nextChar;
                    nextChar = file[filePointer];
                    if (curChar == '\n')
                        lineNumber++;
                }
                filePointer++;
            }
        }

        public Symbol getNextToken()
        {
            if (eof)
                return new Symbol(Tokens.eoft, lineNumber);

            fetchWhile((i) =>
            {
                return Char.IsWhiteSpace(i);
            });

            var t = mapType(curChar);
            Symbol s;
            switch (t)
            {
                case CharTypes.alpha:
                    s = processWord(); 
                    break;
                case CharTypes.numerical:
                    s = processNumber();
                    break;
                case CharTypes.relational:
                    s = processRelOp();
                    fetchChars();
                    break;
                case CharTypes.quote:
                    s = processString();
                    fetchChars();
                    break;
                case CharTypes.whitespace:
                    s = getNextToken();
                    break;
                default: s = 
                    processOther();
                    break;
            }

            return s;
        }

        protected Symbol processWord()
        {
            var lexeme = fetchWhile((i) =>
            {
                return Char.IsLetterOrDigit(i);
            }, 17);

            if (reservedWords.ContainsKey(lexeme))
            {
                return new Symbol(reservedWords[lexeme], lineNumber, lexeme);
            }

            return new Symbol(Tokens.idt, lineNumber, lexeme);   
        }

        protected Symbol processNumber()
        {
            var beforePeriod = fetchWhile((i) =>
            {
                return (Char.IsDigit(i));
            });

            if (curChar != '.')
                return new Symbol(Tokens.numt, lineNumber, null, int.Parse(beforePeriod));

            fetchChars();
            var afterPeriod = fetchWhile((i) =>
            {
                return (Char.IsDigit(i));
            });

            string fullValue = String.Format("{0}.{1}", beforePeriod.ToString(), afterPeriod.ToString());
            return new Symbol(
                Tokens.numt,
                lineNumber,
                null,
                null,
                double.Parse(fullValue)   
            );
        }

        protected Symbol processRelOp()
        {

            if (curChar != '=' && curChar != '#' && nextChar == '=')
            {
                var lexeme = curChar.ToString() + nextChar.ToString();
                fetchChars(2);

                Tokens token = curChar == ':' ? Tokens.assignopt : Tokens.relopt;

                return new Symbol(token, lineNumber, lexeme);
            }
            else if (curChar == '=')
                return new Symbol(Tokens.equalt, lineNumber, curChar.ToString());
            else if (curChar == ':')
                return new Symbol(Tokens.colont, lineNumber, curChar.ToString());

            return new Symbol(Tokens.relopt, lineNumber, curChar.ToString());   
        }

        protected Symbol processOther()
        {
            char[] symbols = new char[] { '{', '}', '[', ']', '`', '~' };
            char[] mulops = new char[] { '*', '/', '&' };
            char[] addops = new char[] { '+', '-' };

            var lexeme = curChar.ToString();

            if (curChar == '.' && char.IsDigit(nextChar))
                return processNumber();

            if (curChar == '(' && nextChar == '*')
            {
                fetchChars(2);
                skipComments();
                return getNextToken();
            }

            if (curChar == -1)
                return new Symbol(Tokens.eoft, lineNumber, lexeme);

            Tokens t;

            if (symbols.Contains(curChar))
                t = Tokens.symbolt;
            else if (mulops.Contains(curChar))
                t = Tokens.mulopt;
            else if (addops.Contains(curChar))
                t = Tokens.addopt;
            else if (curChar == ';')
                t = Tokens.semicolont;
            else if (curChar == '.')
                t = Tokens.periodt;
            else if (curChar == ':')
                t = Tokens.colont;
            else if (curChar == ',')
                t = Tokens.commat;
            else if (curChar == '(')
                t = Tokens.lparent;
            else if (curChar == ')')
                t = Tokens.rparent;
            else
                t = Tokens.unknownt;

            fetchChars();
            return new Symbol(t, lineNumber, lexeme);
        }

        protected Symbol processString()
        {
            char quoteType = curChar;
            fetchChars();
            string lexeme = fetchWhile((i) =>
            {
                return i != quoteType && i != 0 && i != '\n';
            });

            if (curChar == quoteType)
                return new Symbol(Tokens.stringt, lineNumber, lexeme);
            else
                return new Symbol(Tokens.errort, lineNumber, "Error: Unterminated string literal");
        }

        protected void skipComments()
        {
            int commentStack = 1;

            while(commentStack > 0)
            {
                fetchChars();
                if (eof)
                    return;

                else if (curChar == '(' && nextChar == '*')
                {
                    commentStack++;
                    fetchChars(2);
                }

                else if (curChar == '*' && nextChar == ')')
                {
                    commentStack--;
                    fetchChars(2);
                }
            }
        }

        protected string fetchWhile(Matches expression, int charLimit = -1)
        {
            var ret = new StringBuilder();
            while(expression(curChar) && !eof && charLimit != 0)
            {
                ret.Append(curChar);
                fetchChars();
                charLimit--;
            }
            return ret.ToString();
        }

        protected CharTypes mapType(char c)
        {
            if (Char.IsLetter(c))
                return CharTypes.alpha;
            if (Char.IsNumber(c))
                return CharTypes.numerical;
            if (c == '.')
                return CharTypes.period;
            if (c == '<' || c == '>' || c == ':' || c== '=' || c== '#')
                return CharTypes.relational;
            if (c == '\'' || c == '"')
                return CharTypes.quote;
            if (Char.IsWhiteSpace(c))
                return CharTypes.whitespace;

            else return CharTypes.unknown;
        }

        Dictionary<string, Tokens> reservedWords = new Dictionary<string, Tokens>
        {
            { "MODULE", Tokens.modulet },
            { "PROCEDURE", Tokens.proceduret },
            { "VAR", Tokens.vart },
            { "BEGIN", Tokens.begint },
            { "END", Tokens.endt },
            { "IF", Tokens.ift },
            { "THEN", Tokens.thent },
            { "ELSE", Tokens.elset },
            { "ELSIF", Tokens.elseift },
            { "WHILE", Tokens.whilet },
            { "DO", Tokens.dot },
            { "ARRAY", Tokens.arrayt },
            { "RECORD", Tokens.recordt },
            { "CONST", Tokens.constt },
            { "TYPE", Tokens.typet },
            { "OR", Tokens.addopt },
            { "DIV", Tokens.mulopt },
            { "AND", Tokens.addopt },
            { "MOD", Tokens.mulopt },
            { "INTEGER", Tokens.integert },
            { "REAL", Tokens.realt },
            { "CHAR", Tokens.chart }
        };
}

    class Symbol
    {
        public Symbol(Tokens token, int linenumber, string lexeme = null, int? value = null, double? valueR = null)
        {
            this.token = token;
            this.lexeme = lexeme;
            this.value = value;
            this.valueR = valueR;
            lineNumber = linenumber;
        }

        public Tokens token;
        public string lexeme;
        public int lineNumber;
        public int? value;
        public double? valueR;

        public override string ToString()
        {
            string value;

            if (lexeme != null)
                value = lexeme;
            else if (this.value != null)
                value = this.value.ToString();
            else if (this.valueR != null)
                value = this.valueR.ToString();
            else
                value = "<Unknown Token Value>";

            return String.Format(
                "{0, -5}\t{1, -16}\t{2}",
                this.lineNumber.ToString(),
                token.ToString(),
                value
            );
        }
    }
}
