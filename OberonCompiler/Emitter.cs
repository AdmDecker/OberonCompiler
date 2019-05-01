using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    public class Emitter
    {
        SymTable symTable;
        StreamWriter streamWriter;
        int temporaryVariableCounter = 0;

        public Emitter(SymTable symTable, StreamWriter streamWriter)
        {
            this.symTable = symTable;
            this.streamWriter = streamWriter;
        }

        private void EmitLine(string line, params string[] args)
        {
            streamWriter.WriteLine(line, args);
        }

        public void EmitProcedureStart(string procName)
        {
            EmitLine("proc\t{0}", procName);
        }

        public void EmitProcedureEnd(string procName)
        {
            EmitLine("endp {0}", procName);
        }

        public void EmitProgramEnd(string programName)
        {
            EmitLine("START {0}", programName);
        }

        public void EmitReadStatement(List<Token> idlist)
        {
            foreach (Token id in idlist) EmitRead(id);
        }

        private void EmitRead(Token id)
        {
            EmitLine("rdi {0}", getVariableTokenValue(id));
        }

        public void EmitWriteStatement(List<Token> writeList)
        {
            foreach (Token token in writeList) EmitWrite(token);
        }

        public void EmitWriteLnStatement(List<Token> writeList)
        {
            EmitWriteStatement(writeList);
            EmitLine("wrln");
        }

        private void EmitWrite(Token token)
        {
            if (token.type == Tokens.idt)
            {
                // int
                EmitLine("wri {0}", getVariableTokenValue(token));
            }
            else if (token.type == Tokens.numt)
            {
                EmitLine("wri {0}", token.lexeme);
            }
            else if (token.type == Tokens.stringt)
            {
                var sr = symTable.Lookup(token.lexeme);
                EmitLine("wrs {0}", sr.stringRecord.referenceValue);
            }
            else Error.Crash("ERROR IN Emitter.EmitWrite(): unwritable token passed to write procedure");
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
                if (arg.type == Tokens.numt) //LITERAL
                {
                    v = getLiteralTokenValue(arg);
                    VarTypes type = arg.value != null ? VarTypes.intType : VarTypes.floatType;
                    if (type != parameter.type)
                        Error.Crash("Error on line {0}: Mismatched argument type. Type of literal '{1}' is not of type {2}", arg.lineNumber, arg.lexeme, parameter.type.ToString());
                }
                else
                {
                    Record argRecord = symTable.Lookup(arg.lexeme);
                    if (argRecord == null) Error.Crash("Error on line {0}, attempted to call procedure with undeclared argument {1}", arg.lineNumber, arg.lexeme);
                    else if (argRecord.type == RecordTypes.CONSTANT) //CONSTANT
                    {
                        //Verify we don't try to pass constant by reference
                        if (passByReference) Error.Crash("Error on line {0}, attempted to pass CONSTANT value '{1}' by reference", arg.lineNumber, arg.lexeme);
                        v = getConstTokenValue(arg);
                        if (argRecord.constRecord.type != parameter.type)
                            Error.Crash("Error on line {0}: Mismatched argument type. Type of '{1}' is not of type {2}", arg.lineNumber, arg.lexeme, parameter.type.ToString());
                    }
                    else if (argRecord.type == RecordTypes.VARIABLE) //VARIABLE
                    {
                        if (passByReference)
                            v = "@" + arg.lexeme;
                        else v = getVariableTokenValue(arg);

                        if (argRecord.varRecord.type != parameter.type)
                            Error.Crash("Error on line {0}: Mismatched argument type. Type of '{1}' is not of type {2}", arg.lineNumber, arg.lexeme, parameter.type.ToString());
                    }
                    else Error.Crash("Error on line {0}, argument {1} has no value", arg.lineNumber, arg.lexeme);
                }

                EmitLine("push {0}", v);

                parameter = parameter.nextParameter;
            }
            if (parameter != null)
                Error.Crash("Error on line {0}: Not enough arguments in '{1}' function call", procedureToken.lineNumber, procedureToken.lexeme);

            EmitLine("call {0}", procedureToken.lexeme);
        }

        public Token EmitExpression(Token left, Token op, Token right, int depth, ProcedureRecord procedure)
        {
            var record = getTempVarToken(depth, procedure);

            EmitLine("{0} {1} {2} {3} {4}", 
                record.varRecord.getTACString(), 
                "\t=\t",
                getValueString(left),
                op.lexeme,
                getValueString(right));
            return new Token(Tokens.idt, -1, record.symbol.lexeme);
        }

        private Record getTempVarToken(int depth, ProcedureRecord procedure)
        {
            //So here we're looking to turn an expression into a temporary variable
            string tempVarLex = "_t" + temporaryVariableCounter++.ToString();
            //Make our temporary variable as a token
            Token tempVar = new Token(Tokens.idt, -1, tempVarLex);
            var record = symTable.Insert(tempVarLex, tempVar, depth);
            record.varRecord = new VariableRecord(tempVar, VarTypes.intType, symTable.offset, 2, depth, false, false);

            if (procedure != null)
                procedure.AddLocal(record.varRecord);

            symTable.incrementOffset(2);
            return record;
        }

        public Token EmitBuffer(Token bufferedVariable, int depth, ProcedureRecord procedure)
        {
            var token = getTempVarToken(depth, procedure).symbol;
            EmitAssignment(token, bufferedVariable);
            return token;
        }

        public void EmitAssignment(Token left, Token right)// a := _tX
        {
            EmitLine("{0}\t=\t{1}", getValueString(left), getValueString(right));
        }

        public Token EmitNegation(Token t, int depth, ProcedureRecord procedure)
        {
            //My expression: 0 - t
            return EmitExpression(
                new Token(Tokens.numt, -1, "0", 0, 0),
                new Token(Tokens.minust, -1, "-"),
                t,
                depth,
                procedure);
        }

        private string getValueString(Token token)
        {
            if (token.type == Tokens.idt)
            {
                var vr = symTable.Lookup(token.lexeme);
                if (vr == null)
                    Error.Crash("Error on line {0}: Use of undeclared identifier {1}", token.lineNumber, token.lexeme);
                if (vr.type == RecordTypes.CONSTANT)
                    return getConstTokenValue(token);
                else if (vr.type == RecordTypes.VARIABLE)
                    return getVariableTokenValue(token);
                else Error.Crash("Error on line {0}: Use of invalid identifier {1} in expression", token.lineNumber, token.lexeme);
            }
                
                
            else if (token.type == Tokens.numt)
                return getLiteralTokenValue(token);

            Error.Crash("Error on line {0}: Unexpected token '{1}' in expression emission", token.lineNumber, token.lexeme);
            return "";
        }

        private string getVariableTokenValue(Token token)
        {
            var symbol = symTable.Lookup(token.lexeme);
            if (symbol != null && symbol.varRecord != null)
            {
                return symbol.varRecord.getTACString();
            }

            Error.Crash("Error on line {0}: Use of undeclared variable {1}", token.lineNumber, token.lexeme);
            return "";
        }

        private string getConstTokenValue(Token token)
        {
            var symbol = symTable.Lookup(token.lexeme);
            if (symbol != null && symbol.constRecord != null)
            {
                return symbol.constRecord.getTACString();
            }

            Error.Crash("Error on line {0}: Use of undeclared constant {1}", token.lineNumber, token.lexeme);
            return "";
        }

        private string getLiteralTokenValue(Token token)
        {
            var obj = new LiteralValue(token.lexeme);
            return obj.getTACString();
        }
    }
}
