using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    public static class TokenFactory
    {
        public static Token createEmptyToken()
        {
            return new Token(Tokens.emptyt, -1);
        }
    }
}
