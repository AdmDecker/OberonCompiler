using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    enum tokens { numt, stringt, }

    class Analyzer
    {
        List<string> ReservedWords = new List<string> {
            "MODULE", "PROCEDURE", "VAR", "BEGIN",
            "END", "IF", "THEN", "ELSE", "ELSIF",
            "WHILE", "DO", "ARRAY", "RECORD", "CONST",
            "TYPE" };

    }
}
