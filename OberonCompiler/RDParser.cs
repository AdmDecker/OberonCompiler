using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    //Recursive descent parser
    public class RDParser
    {
        //External component references
        IAnalyzer a;
        SymTable symTable;
        Emitter e;


        private Token ct; //Current type
        private Token nt; //Next type
        private int depth = 0;
        Stack<string> headingLexes = new Stack<string>();

        public void Goal(IAnalyzer a, SymTable s, Emitter e)
        {
            this.e = e;
            this.a = a;
            this.symTable = s;
            this.nt = a.getNextToken();

            Prog();
            symTable.WriteTable(0);
            Match(Tokens.eoft);
            Console.WriteLine("Parse completed successfully");
        }

        private void PopHeadingLex(Token t)
        {
            string pop = "";
            string failMessage = string.Format("Error on line {0}, end statement with no matching begin",
                    t.lineNumber);
            try
            {
                pop = headingLexes.Pop();
            }
            catch
            {
                Error.Crash(failMessage);
            }

            Record rec = symTable.Lookup(t.lexeme);
            if (rec == null)
                Error.Crash(failMessage);

            if (t.lexeme != pop)
                Error.Crash("Error on line {0}: end statement does not match '{2}' declared on line {3}",
                    t.lineNumber, rec.symbol.lexeme, rec.symbol.lineNumber);
        }

        private void Match(params Tokens[] t)
        {
            bool success = Peek(t);
            CycleTokens();

            if (!success)
            {
                string s = "";
                foreach(Tokens f in t)
                {
                    s += '\n';
                    s += f.ToString();
                }
                Error.Crash(
                    "Parse Error: Unexpected token '{0}' of type {3} on line {1}, expected one of the following types: {2}",
                    ct.lexeme, ct.lineNumber, s, ct.type.ToString());
            }
        }

        private bool Peek(params Tokens[] t)
        {
            foreach (Tokens b in t)
            {
                if (nt.type == b)
                    return true;
            }

            return false;
        }

        private bool PeekOrMatch(params Tokens[] t)
        {
            bool peek = Peek(t);
            if (peek)
                Match(t);
            return peek;
        }

        private void CycleTokens()
        {
            this.ct = this.nt;
            this.nt = a.getNextToken();
        }

        private void Prog()
        {
            //Prog->modulet idt semicolont DeclarativePart 
            //      StatementPart endt idt periodt
            Match(Tokens.modulet);
            Match(Tokens.idt);
            var programName = ct.lexeme;
            var record = symTable.Insert(ct.lexeme, ct, 0);
            record.procRecord = new ProcedureRecord();
            headingLexes.Push(ct.lexeme);
            Match(Tokens.semicolont);
            DeclarativePart();
            StatementPart(record);
            Match(Tokens.endt);
            Match(Tokens.idt);
            PopHeadingLex(ct);
            e.EmitProcedureEnd(programName);
            Match(Tokens.periodt);
            e.EmitProgramEnd(programName);
        }

        private void DeclarativePart(ProcedureRecord procedure = null)
        {
            //DeclarativePart -> ConstPart VarPart ProcPart
            ConstPart(procedure);
            VarPart(procedure);
            ProcPart();
        }

        private void ConstPart(ProcedureRecord procedure)
        {
            //ConstPart -> constt ConstTail
            if (nt.type == Tokens.constt)
            {
                Match(Tokens.constt);
                ConstTail(procedure);
            }
            //ConstPart -> emptyt
        }

        private void ConstTail(ProcedureRecord procedure)
        {
            //ConstTail -> idt equalt Value semicolont ConstTail
            if (nt.type == Tokens.idt)
            {
                Match(Tokens.idt);
                var rec = symTable.Insert(ct.lexeme, ct, depth);
                Match(Tokens.equalt);
                var s = Value();
                rec.constRecord = new ConstantRecord(s);
                Match(Tokens.semicolont);
                if (procedure != null)
                {
                    procedure.AddLocal(rec.constRecord);
                }

                ConstTail(procedure);
            }
            //ConstTail -> emptyt
        }

        private void VarPart(ProcedureRecord procedure)
        {
            symTable.resetOffset();
           //VarPart->vart VarTail
           if (nt.type == Tokens.vart)
           {
              Match(Tokens.vart);
              VarTail(procedure);
           }
           //VarPart -> emptyt
        }

        private void VarTail(ProcedureRecord procedure)
        {
            //VarTail -> IdentifierList colont TypeMark semicolont VarTail
            if (nt.type == Tokens.idt)
            {
                var list = IdentifierList();
                Match(Tokens.colont);
                var type = TypeMark();
                Match(Tokens.semicolont);

                int varSize = type == VarTypes.floatType ? 4 : 2;
                foreach (var id in list)
                {
                    var entry = symTable.Lookup(id);
                    entry.varRecord = new VariableRecord(entry.symbol, type, symTable.offset, varSize, depth, false, false);
                    symTable.incrementOffset(varSize);
                    if(procedure != null)
                    {
                        procedure.AddLocal(entry.varRecord);
                    }
                }
                VarTail(procedure);
            }
            //VarTail -> emptyt
        }

        private List<string> IdentifierList()
        {
            //IdentifierList -> idt IdentifierList'
            Match(Tokens.idt);
            symTable.Insert(ct.lexeme, ct, depth);
            string lex = ct.lexeme;
            var list = IdentifierList2();
            list.Insert(0, lex);
            return list;
        }

        private List<string> IdentifierList2()
        {
            List<string> ids = new List<string>();
            //IdentifierList' -> commat idt IdentifierList'
            if (nt.type == Tokens.commat)
            {
                Match(Tokens.commat);
                Match(Tokens.idt);
                symTable.Insert(ct.lexeme, ct, depth);
                ids.Add(ct.lexeme);
                ids.AddRange(IdentifierList2());
            }
            //IdentifierList' -> emptyt
            return ids;
        }

        private VarTypes TypeMark()
        {
            //TypeMark -> integert
            //TypeMark->realt
            //TypeMark->chart
            Match(Tokens.integert, Tokens.chart, Tokens.realt);
            switch(ct.type)
            {
                case Tokens.integert:
                    return VarTypes.intType;
                case Tokens.chart:
                    return VarTypes.charType;
                case Tokens.realt:
                    return VarTypes.floatType;
            }
            throw new Exception();
        }

        private Token Value()
        {
            //Value -> numt
            Match(Tokens.numt);
            return ct;
        }

        private void ProcPart()
        {
            //ProcPart->ProcedureDecl ProcPart
            if (nt.type == Tokens.proceduret)
            {
                ProcedureDecl();
                ProcPart();
            }
            //ProcPart->emptyt
        }

        private void ProcedureDecl()
        {
            //ProcedureDecl -> ProcHeading semicolont ProcBody idt semicolont
            var record = ProcHeading();
            headingLexes.Push(record.symbol.lexeme);
            Match(Tokens.semicolont);
            ProcBody(record);

            //popperino
            Match(Tokens.idt);
            PopHeadingLex(ct);

            Match(Tokens.semicolont);

            symTable.WriteTable(depth);
            symTable.DeleteDepth(depth);
            depth--;
            e.EmitProcedureEnd(record.symbol.lexeme);
        }

        private Record ProcHeading()
        {
            //ProcHeading -> proceduret idt Args
            Match(Tokens.proceduret);
            Match(Tokens.idt);
            var rec = symTable.Insert(ct.lexeme, ct, depth);
            rec.procRecord = new ProcedureRecord();
            depth++;
            Args(ct.lexeme);
            return rec;
        }

        private void ProcBody(Record record)
        {
            //ProcBody -> DeclarativePart StatementPart endt
            DeclarativePart(record.procRecord);
            StatementPart(record);
            Match(Tokens.endt);
        }

        private void Args(string procLex)
        {
            //Args -> lparent ArgList rparent
            if(nt.type == Tokens.lparent)
            {
                Match(Tokens.lparent);
                var list = ArgList(procLex);
                Match(Tokens.rparent);

                //update arg offsets
                var procRec = symTable.Lookup(procLex);
                int offset = procRec.procRecord.sizeOfParameters;
                foreach( var id in list )
                {
                    var rec = symTable.Lookup(id);
                    rec.varRecord.offset = offset;
                    offset -= rec.varRecord.size; 
                }
            }
            //Args -> emptyt
        }

        private List<string> ArgList(string procLex)
        {
            Record procRec = symTable.Lookup(procLex);
            // ArgList->Mode IdentifierList colont TypeMark MoreArgs
            bool hasVarMode = Mode();
            var list = IdentifierList();
            Match(Tokens.colont);
            var type = TypeMark();

            foreach(var id in list)
            {
                int size = type == VarTypes.floatType ? 4 : 2;
                var vRec = symTable.Lookup(id);
                var rec = new VariableRecord(vRec.symbol, type, 0, size, depth, true, hasVarMode);
                procRec.procRecord.AddParameter(rec, hasVarMode);
                vRec.varRecord = rec;
            }
            
            list.AddRange(MoreArgs(procLex));
            return list;
        }

        private List<string> MoreArgs(string procLex)
        {
            //MoreArgs -> semicolont ArgList
            if (nt.type == Tokens.semicolont)
            {
                Match(Tokens.semicolont);
                return ArgList(procLex);
            }
            //MoreArgs -> emptyt
            return new List<string>();
        }

        private bool Mode()
        {
            //Mode -> vart
            if (nt.type == Tokens.vart)
            {
                Match(Tokens.vart);
                return true;
            }
            //Mode -> emptyt
            return false;
        }

        private void StatementPart(Record procedure)
        {
            //StatementPart -> begint SeqOfStatements
            if (PeekOrMatch(Tokens.begint))
            {
                e.EmitProcedureStart(procedure.symbol.lexeme);
                SeqOfStatements(procedure.procRecord);
            }
            //StatementPart -> emptyt
        }

        private void SeqOfStatements(ProcedureRecord procedure)
        {
            //SeqOfStatements -> Statement ; StatTail | e
            if (Peek(Tokens.idt, Tokens.readt, Tokens.writelnt, Tokens.writet))
            {
                Statement(procedure);
                Match(Tokens.semicolont);
                StatTail(procedure);
            }
        }

        private void StatTail(ProcedureRecord procedure)
        {
            //StatTail -> Statment ; StatTail | e
            if (Peek(Tokens.idt, Tokens.readt, Tokens.writelnt, Tokens.writet))
            {
                Statement(procedure);
                Match(Tokens.semicolont);
                StatTail(procedure);
            }
        }

        private void Statement(ProcedureRecord procedure)
        {
            //Statement -> AssignStat | IOStat
            if (Peek(Tokens.idt))
            {
                AssignStat(procedure);
            }
            else
                IOStat();
        }

        private void IOStat()
        {
            //IO_Stat -> In_Stat | Out_Stat
            if (Peek(Tokens.readt))
                In_Stat();
            else
                Out_Stat();
        }

        private void In_Stat()
        {
            //In_Stat -> read(Id_List)
            Match(Tokens.readt);
            Match(Tokens.lparent);
            e.EmitReadStatement(Id_List());
            Match(Tokens.rparent);
        }

        private List<Token> Id_List()
        {
            var idlist = new List<Token>();
            //Id_List -> idt Id_List_Tail
            Match(Tokens.idt);
            idlist.Add(ct);
            idlist.AddRange(Id_List_Tail());
            return idlist;
        }

        private List<Token> Id_List_Tail()
        {
            var idlist = new List<Token>();
            //Id_List_Tail -> , idt Id_List_Tail | e
            if (PeekOrMatch(Tokens.commat))
            {
                Match(Tokens.idt);
                idlist.Add(ct);
                idlist.AddRange(Id_List_Tail());
            }
            //e
            return idlist;
        }

        private void Out_Stat()
        {
            //Out_Stat -> write(Write_List) | writeln(Write_List)
            if (PeekOrMatch(Tokens.writet))
            {
                Match(Tokens.lparent);
                var writelist = Write_List();
                e.EmitWriteStatement(writelist);
                Match(Tokens.rparent);
            }
            else if (PeekOrMatch(Tokens.writelnt))
            {
                Match(Tokens.lparent);
                var writelist = Write_List();
                e.EmitWriteLnStatement(writelist);
                Match(Tokens.rparent);
            }
            else Match(Tokens.writet, Tokens.writelnt); //this will fail
        }

        private List<Token> Write_List()
        {
            var writeList = new List<Token>();
            //Write_List -> Write_Token Write_List_Tail
            writeList.Add(Write_Token());
            writeList.AddRange(Write_List_Tail());
            return writeList;
        }

        private List<Token> Write_List_Tail()
        {
            var writeList = new List<Token>();
            //Write_List_Tail -> , Write_Token Write_List_Tail | e
            if (PeekOrMatch(Tokens.commat))
            {
                writeList.Add(Write_Token());
                writeList.AddRange(Write_List_Tail());
            }
            //e
            return writeList;
        }

        private Token Write_Token()
        {
            //Write_Token -> idt | numt | literal
            Match(Tokens.idt, Tokens.numt, Tokens.stringt);
            if (ct.type == Tokens.stringt)
            {
                var s = symTable.Insert(ct.lexeme, ct, 0); //Strings are always base depth
                s.stringRecord = new StringLiteral(ct.lexeme);
            }
            return ct;
        }

        private Token Expr(ProcedureRecord procedure) //returns temp var
        {
            //Expr -> Relation
            return Relation(procedure);
        }

        private Token Relation(ProcedureRecord procedure) // returns temp var
        {
            //Relation -> SimpleExpr
            return SimpleExpr(procedure);
        }

        private Token SimpleExpr(ProcedureRecord procedure) // returns temp var
        {
            //SimpleExpr -> Term MoreTerm
            var l = Term(procedure);
            return MoreTerm(l, procedure);
        }

        private Token MoreTerm(Token synth, ProcedureRecord procedure)
        {
            //MoreTerm -> Addop Term MoreTerm | e
            if (Peek(Tokens.addopt, Tokens.minust))
            {
                var op = Addop();
                var l = Term(procedure);
                var r = MoreTerm(l, procedure);
                return e.EmitExpression(synth, op, r, depth, procedure);
            }
            return synth;
        }

        private Token Term(ProcedureRecord procedure)
        {
            //Term -> Factor MoreFactor
            var l = Factor(procedure);
            return MoreFactor(l, procedure);
        }

        private Token MoreFactor(Token synth, ProcedureRecord procedure)
        {
            //MoreFactor -> Mulop Factor MoreFactor | e
            if (Peek(Tokens.mulopt))
            {
                var op = Mulop();
                var l = Factor(procedure);
                var r = MoreFactor(l, procedure);
                return e.EmitExpression(synth, op, r, depth, procedure); // throw away L because it got synthed into R anyway
            }
            return synth;
        }

        private Token Factor(ProcedureRecord procedure) //returns the THING
        {
            //Factor -> idt | numt | ( Expr ) | ~ Factor | SignOp Factor
            switch (nt.type)
            {
                case Tokens.idt:
                    Match(Tokens.idt);
                    return ct;
                case Tokens.numt:
                    Match(Tokens.numt);
                    return ct;
                case Tokens.lparent:
                    Match(Tokens.lparent);
                    var tempVar = Expr(procedure);
                    Match(Tokens.rparent);
                    return tempVar;
                case Tokens.tildet:
                    Match(Tokens.tildet);
                    return Factor(procedure);
                case Tokens.minust:
                    SignOp();
                    var factor = Factor(procedure);
                    return e.EmitNegation(factor, depth, procedure);
                default:
                    Match(Tokens.idt, Tokens.numt, Tokens.lparent, Tokens.tildet, Tokens.minust);
                    return TokenFactory.createEmptyToken();
            }
        }

        private Token Addop()
        {
            //Addop -> + | - | OR
            //Tokens.addopt = + | OR
            Match(Tokens.addopt, Tokens.minust);
            return ct;
        }

        private Token Mulop()
        {
            //Mulop -> * | / | DIV | MOD | &
            //Tokens.mulopt = DIV | MOD | * | / | &
            Match(Tokens.mulopt);
            return ct;
        }

        private void SignOp()
        {
            //SignOp -> -
            Match(Tokens.minust);
        }

        private void AssignStat(ProcedureRecord procedure)
        {
            //AssignStat -> idt := Expr | ProcCall

            Match(Tokens.idt);
            //Match idt
            if (Peek(Tokens.lparent))
            {
                ProcCall();
            }
            else if (Peek(Tokens.assignopt))
            {
                var symbol = symTable.Lookup(ct.lexeme);
                if (symbol == null)
                {
                    Error.Crash("Error on line {0}: Undeclared variable {1} used in assignment statment",
                        ct.lineNumber, ct.lexeme);
                }
                var l = ct;
                Match(Tokens.assignopt);
                var expr = Expr(procedure);
                var buffer = e.EmitBuffer(expr, depth, procedure);
                e.EmitAssignment(l, buffer);
            }
            else Match(Tokens.lparent, Tokens.assignopt);
        }

        private void ProcCall()
        {
            //ProcCall -> idt ( Params )

            //Already matched idt in AssignStat(), so skip
            var symbol = symTable.Lookup(ct.lexeme);
            if (symbol == null)
            {
                Error.Crash("Error on line {0}: Attempted to call undeclared procedure '{1}'",
                    ct.lineNumber, ct.lexeme);
            }
            if (symbol.type == RecordTypes.PROCEDURE)
            {
                Match(Tokens.lparent);
                var args = Params();
                Match(Tokens.rparent);

                e.EmitProcedureCall(symbol.symbol, args.ToArray());
            }
            else
                Error.Crash("Error on line {0}, attempted to call non-proc symbol {1}",
                    ct.lineNumber, ct.lexeme);
        }

        private List<Token> Params()
        {
            List<Token> args = new List<Token>();
            //Params -> idt ParamsTail | numt ParamsTail | e
            if (PeekOrMatch(Tokens.numt, Tokens.idt))
            {
                args.Add(ct);
                args.AddRange(ParamsTail());
            }
            return args;
        }

        private List<Token> ParamsTail()
        {
            var args = new List<Token>();
            //ParamsTail -> , idt ParamsTail | , num ParamsTail | e
            if (PeekOrMatch(Tokens.commat))
            {
                Match(Tokens.idt, Tokens.numt);
                args.Add(ct);
                args.AddRange(ParamsTail());
            }
            return args;
        }
    }
}
