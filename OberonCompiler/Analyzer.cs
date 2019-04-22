using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{

    public interface IAnalyzer
    {
        Token getNextToken();
    }
    
    public enum Tokens {
        modulet, proceduret, vart, begint, endt, ift, thent, elset, elseift, whilet, dot, arrayt,
        recordt, constt, typet, idt, addopt, mulopt, numt, relopt, assignopt, symbolt, unknownt, eoft,
        stringt, errort, emptyt, commat, semicolont, colont, periodt, equalt, lparent, rparent, integert,
        realt, chart, minust, tildet, readt, writet, writelnt
    }

    public enum CharTypes { alpha, numerical, period, relational, math, unknown, eof, quote,
        whitespace
    }

    public class Analyzer : IAnalyzer
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

        public Token getNextToken()
        {
            if (eof)
                return new Token(Tokens.eoft, lineNumber);

            fetchWhile((i) =>
            {
                return Char.IsWhiteSpace(i);
            });

            var t = mapType(curChar);
            Token s;
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

        protected Token processWord()
        {
            var lexeme = fetchWhile((i) =>
            {
                return Char.IsLetterOrDigit(i);
            }, 17);

            if (reservedWords.ContainsKey(lexeme))
            {
                return new Token(reservedWords[lexeme], lineNumber, lexeme);
            }

            return new Token(Tokens.idt, lineNumber, lexeme);   
        }

        protected Token processNumber()
        {
            var beforePeriod = fetchWhile((i) =>
            {
                return (Char.IsDigit(i));
            });

            if (curChar != '.')
                return new Token(Tokens.numt, lineNumber, beforePeriod, int.Parse(beforePeriod));

            fetchChars();
            var afterPeriod = fetchWhile((i) =>
            {
                return (Char.IsDigit(i));
            });

            string fullValue = String.Format("{0}.{1}", beforePeriod.ToString(), afterPeriod.ToString());
            return new Token(
                Tokens.numt,
                lineNumber,
                fullValue,
                null,
                double.Parse(fullValue)   
            );
        }

        protected Token processRelOp()
        {

            if (curChar != '=' && curChar != '#' && nextChar == '=')
            {
                var lexeme = curChar.ToString() + nextChar.ToString();
                Tokens token = curChar == ':' ? Tokens.assignopt : Tokens.relopt;

                fetchChars();

                return new Token(token, lineNumber, lexeme);
            }
            else if (curChar == '=')
                return new Token(Tokens.equalt, lineNumber, curChar.ToString());
            else if (curChar == ':')
                return new Token(Tokens.colont, lineNumber, curChar.ToString());

            return new Token(Tokens.relopt, lineNumber, curChar.ToString());   
        }

        protected Token processOther()
        {
            char[] symbols = new char[] { '{', '}', '[', ']', '`', '~' };
            char[] mulops = new char[] { '*', '/', '&' };
            char[] addops = new char[] { '+'};

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
                return new Token(Tokens.eoft, lineNumber, lexeme);

            Tokens t;

            if (curChar == '~')
                t = Tokens.tildet;
            if (curChar == '-')
                t = Tokens.minust;
            else if (symbols.Contains(curChar))
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
            return new Token(t, lineNumber, lexeme);
        }

        protected Token processString()
        {
            char quoteType = curChar;
            fetchChars();
            string lexeme = fetchWhile((i) =>
            {
                return i != quoteType && i != 0 && i != '\n';
            });

            if (curChar == quoteType)
                return new Token(Tokens.stringt, lineNumber, lexeme);
            else
                return new Token(Tokens.errort, lineNumber, "Error: Unterminated string literal");
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
            { "CHAR", Tokens.chart },
            { "READ", Tokens.readt },
            { "WRITE", Tokens.writet },
            { "WRITELN", Tokens.writelnt },
        };
}

    public class Token
    {
        public Token(Tokens token, int linenumber, string lexeme = null, int? value = null, double? valueR = null)
        {
            this.type = token;
            this.lexeme = lexeme;
            this.value = value;
            this.valueR = valueR;
            lineNumber = linenumber;
        }

        public Tokens type;
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
                type.ToString(),
                value
            );
        }
    }
}
