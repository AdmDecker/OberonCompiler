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

        public void EmitProcedureCall(Token procedureToken, params Token [] argLexemes)
        {
            Record record = symTable.Lookup(procedureToken.lexeme);
            if (record == null) Error.Crash("Error on line {0}, attempted to call undeclared procedure {1}", procedureToken.lineNumber, procedureToken.lexeme);
            if (record.procRecord == null) Error.Crash("Error on line {0}, attempted to call non-procedure {1}", procedureToken.lineNumber, procedureToken.lexeme);

            var parameter = record.procRecord.parameters;

            foreach(Token arg in argLexemes)
            {
                if (parameter == null) Error.Crash("Error on line {0}, too many arguments in procedure call", procedureToken.lineNumber);
                bool passByReference = parameter.modeIsVar;
                string v = "ERROR IN Emmitter.EmitProcedureCall(): CODE PATH DID NOT ASSIGN 'V'";
                Record argRecord = symTable.Lookup(arg.lexeme);
                if (argRecord == null) Error.Crash("Error on line {0}, attempted to call procedure with undeclared argument {1}", arg.lineNumber, arg.lexeme);

                if (arg.type == Tokens.numt) //LITERAL
                {
                    v = arg.lexeme;
                }
                else if (argRecord.type == RecordTypes.CONSTANT) //CONSTANT
                {
                    //Verify we don't try to pass constant by reference
                    if (passByReference) Error.Crash("Error on line {0}, attempted to pass CONSTANT value '{1}' by reference", arg.lineNumber, arg.lexeme);
                    v = arg.lexeme;
                }
                else if (argRecord.type == RecordTypes.VARIABLE) //VARIABLE
                {
                    if (passByReference)
                        v = "@" + arg.lexeme;
                    else v = arg.lexeme;
                }
                else Error.Crash("Error on line {0}, argument {1} has no value", arg.lineNumber, arg.lexeme);

                EmitLine("push {0}", v);

                parameter = parameter.nextParameter;
            }

            EmitLine("call {0}", procedureToken.lexeme);
        }

        public string EmitExpression(string left, Token lexemeOp, string lexemeRight)
        {
            
        }

        public void EmitAssignment(string lexemeLeft, string lexemeRight)// a := _tX
        {
        }
    }
}
