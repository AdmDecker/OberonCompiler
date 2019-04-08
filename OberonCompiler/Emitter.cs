using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    public class Emitter
    {
        SymTable symTable;
        int temporaryVariableCounter = 0;

        public Emitter(SymTable symTable)
        {
            this.symTable = symTable;
        }

        private void EmitLine(string line, params string[] args)
        {
            
        }

        public void EmitProcedureStart(string procName)
        {
            EmitLine("proc\t{0}", procName);
        }

        public void EmitProcedureEnd(string procName)
        {
            EmitLine("endp {0}", procName);
        }

        public void EmitProcedureCall(string procedureLexeme, params string [] argLexemes)
        {
            Record rec = symTable.Lookup(procedureLexeme);
            if (rec == null)

            foreach(string arg in argLexemes)
            {
                string v = arg;
                if (arg.isReference)
                    v = "@" + v;
                EmitLine("push {0}", v);
            }

            EmitLine("call {0}", procedureLexeme);
        }

        public string EmitExpression(string lexemeLeft, string lexemeOp, string lexemeRight)
        {
            
        }

        public void EmitAssignment(string lexemeLeft, string lexemeRight)// a := _tX
        {
        }
    }
}
