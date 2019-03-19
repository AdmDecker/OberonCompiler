using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    interface IParserHooks
    {
        void signalVariable(string variable);
        void cancelVariable(string variable);
        void endVariable(string variable);
        void writeToken(string token);
    }

    enum TrackVariables
    {
        ConstTail, VarTail, TypeMark, IdentifierList, ProcHeading, Untracked
    }

    //A state machine for writing data from Parser to SymTable
    class ParserHooks
    {
        SymTable symTable;

        //Current working const lexeme
        string constLex;

        //Push Identifier lists into public space so it can be grabbed by a variable
        List<string> idList = new List<string>();
        Tokens idListTypeMark;
        bool isVarMode = false;

        //Push TypeMark into public space so it can be grabbed by a variable
        VarTypes typeMark;

        DepthTracker dt = new DepthTracker();
        Stack<TrackVariables> varState = new Stack<TrackVariables>();

        //Procedures can be nested so we need to keep a stack of them
        Stack<string> procStack = new Stack<string>();

        ParserHooks(SymTable symTable)
        {
            this.symTable = symTable;
        }


        void signalVariable(string variable)
        {
            var tvar = mapVar(variable);

            switch(tvar)
            {
                case TrackVariables.IdentifierList:
                    this.idList.Clear();
                    break;
            }

            if (tvar != TrackVariables.Untracked)
                varState.Push(tvar);
        }

        void cancelVariable(string variable)
        {
            var tvar = mapVar(variable);
            if (tvar != TrackVariables.Untracked)
                varState.Pop();

            //symTable.DeleteDepth(depth--);
        }

        void endVariable(string variable)
        {
            var tvar = mapVar(variable);
            if (tvar != TrackVariables.Untracked)
                varState.Pop();

            if (variable == "ProcedureDecl")
            {
                //Write symTable
                symTable.WriteTable(dt.getDepth(), true);
                symTable.DeleteDepth(dt.getDepth());
                dt.decrementDepth();
            }
            else if (variable == "Prog")
                symTable.WriteTable(1);
        }

        void writeToken(Symbol token)
        {
            switch(varState.Peek())
            {
                case TrackVariables.ConstTail:
                    resolveConstToken(token);
                    break;
                case TrackVariables.TypeMark:
                    resolveTypeMarkToken(token);
                    break;
                case TrackVariables.IdentifierList:
                    resolveIdListToken(token);
                    break;
                case TrackVariables.VarTail:
                    resolveVarTailToken(token);
                    break;
                default:
                    return;
            }
        }

        //ArgList -> Mode[vart] IdentifierList colont TypeMark MoreArgs
        void resolveArgListToken(Symbol token)
        {
            switch(token.token)
            {
                case Tokens.vart:
                    this.isVarMode = true;
                    break;
                case Tokens.emptyt:
                case Tokens.semicolont:
                    var proc = symTable.Lookup(procStack.Peek());
                    //We're stepping into MoreArgs, so flush this set
                    foreach(var id in idList)
                    {
                        var rec = symTable.Lookup(id);
                        rec.varRecord = new VariableRecord(typeMark, dt.getOffset(typeMark), dt.getSize(typeMark));
                        proc.procRecord.AddParameter(rec.varRecord, this.isVarMode);
                    }
                    //clear varmode flag for next set of ArgList
                    this.isVarMode = false;
                    break;
            }
        }

        //ProcHeading -> proceduret idt Args
        void resolveProcHeadingToken(Symbol token)
        {
            switch(token.token)
            {
                case Tokens.idt:
                    var rec = symTable.Lookup(token.lexeme);
                    if (rec != null && rec.depth == dt.getDepth())
                    {
                        Console.WriteLine("Error on Line {0}, procedure already declared in this scope: {1}",
                            token.lineNumber, token.lexeme);
                    }
                    procStack.Push(token.lexeme);
                    var proc = symTable.Insert(token.lexeme, token, dt.getDepth());
                    proc.procRecord = new ProcedureRecord();
                    dt.incrementDepth();
                    break;
            }
        }

        void resolveTypeMarkToken(Symbol token)
        {
            switch(token.token)
            {
                case Tokens.integert:
                    this.typeMark = VarTypes.intType; break;
                case Tokens.realt:
                    this.typeMark = VarTypes.floatType; break;
                case Tokens.chart:
                    this.typeMark = VarTypes.charType; break;
            }
        }

        void resolveIdListToken(Symbol token)
        {
            if (token.token == Tokens.idt)
            {
                idList.Add(token.lexeme);
                Record rec = symTable.Lookup(token.lexeme);
                if (rec != null && rec.depth == dt.getDepth())
                {
                    Console.WriteLine("Error on Line {0}, variable already declared in this scope: {1}",
                        token.lineNumber, token.lexeme);
                    return;
                }
                symTable.Insert(token.lexeme, token, dt.getDepth());
            }
                
        }

        void resolveVarTailToken(Symbol token)
        {
            Record rec;
            //Add data to current Const value dependent on token type
            switch (token.token)
            {
                case Tokens.idt:
                    break;
                case Tokens.semicolont:
                    Record proc = null;
                    var isLocal = procStack.Count > 0;
                    if (isLocal)
                        proc = symTable.Lookup(procStack.Peek());
                    foreach (string s in idList)
                    {
                        rec = symTable.Lookup(s);
                        rec.varRecord = new VariableRecord(typeMark, dt.getOffset(typeMark), dt.getSize(typeMark));
                        if (isLocal)
                        {
                            //Insert to Procedure
                            proc.procRecord.AddLocal(rec.varRecord);
                        }
                    }
                    break;
            }
        }

        void resolveConstToken(Symbol token)
        {
            Record rec;
            //Add data to current Const value dependent on token type
            switch(token.token)
            {
                case Tokens.idt:
                    rec = symTable.Lookup(this.constLex);
                    if (rec != null && rec.depth == dt.getDepth())
                    {
                        Console.WriteLine("Error on Line {0}, const variable already declared in this scope: {1}",
                            token.lineNumber, token.lexeme);
                        return;
                    }
                    this.constLex = token.lexeme;
                    symTable.Insert(constLex, token, dt.getDepth());
                    break;
                case Tokens.numt:
                    rec = symTable.Lookup(this.constLex);
                    if (rec != null)
                        rec.constRecord = new ConstantRecord(token);
                    break;
            }
        }


        TrackVariables mapVar(string var)
        {
            if (var == "ConstTail")
                return TrackVariables.ConstTail;
            else if (var == "VarTail")
                return TrackVariables.VarTail;
            else if (var == "IdentifierList")
                return TrackVariables.IdentifierList;
            else if (var == "TypeMark")
                return TrackVariables.TypeMark;
            else if (var == "ProcHeading")
                return TrackVariables.ProcHeading;
            else
                return TrackVariables.Untracked;
        }
    }

    class DepthTracker
    {
        int depth = 1;
        int offset = 0;

        public int getDepth()
        {
            return depth;
        }

        public void incrementDepth()
        {
            depth++;
            offset = 0;
        }

        public int getOffset(VarTypes t)
        {
            var ret = offset;
            offset += getSize(t);
            return ret;
        }

        public int getSize(VarTypes t)
        {
            switch (t)
            {
                case VarTypes.charType:
                case VarTypes.intType:
                    return 2;
                case VarTypes.floatType:
                    return 4;
            }

            return 0;
        }
    }
}
