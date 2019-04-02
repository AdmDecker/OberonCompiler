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
        private Token ct; //Current type
        private Token nt; //Next type
        IAnalyzer a;
        SymTable symTable;
        int offset = 0;
        private int depth = 0;
        Stack<string> headingLexes = new Stack<string>();

        public void Goal(IAnalyzer a, SymTable s)
        {
            this.a = a;
            this.symTable = s;
            this.nt = a.getNextToken();
            Prog();
            symTable.WriteTable(0);
            Match(Tokens.eoft);
            Console.WriteLine("Parse completed successfully");
        }

        private void Crash(string format, params object[] args)
        {
            Console.WriteLine(format, args);
            Console.ReadLine();
            Environment.Exit(1);
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
                Crash(failMessage);
            }

            Record rec = symTable.Lookup(t.lexeme);
            if (rec == null)
                Crash(failMessage);

            if (t.lexeme != pop)
                Crash("Error on line {0}: end statement does not match '{2}' declared on line {3}",
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
                Crash(
                    "Parse Error: Unexpected type '{0}' of type {3} on line {1}, expected type of type(s): {2}",
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
            symTable.Insert(ct.lexeme, ct, 0);
            headingLexes.Push(ct.lexeme);
            Match(Tokens.semicolont);
            DeclarativePart();
            StatementPart();
            Match(Tokens.endt);
            Match(Tokens.idt);
            PopHeadingLex(ct);
            Match(Tokens.periodt);
        }

        private void DeclarativePart(string procLex = "")
        {
            //DeclarativePart -> ConstPart VarPart ProcPart
            ConstPart(procLex);
            VarPart(procLex);
            ProcPart();
        }

        private void ConstPart(string procLex)
        {
            //ConstPart -> constt ConstTail
            if (nt.type == Tokens.constt)
            {
                Match(Tokens.constt);
                ConstTail(procLex);
            }
            //ConstPart -> emptyt
        }

        private void ConstTail(string procLex)
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
                if (procLex != "")
                {
                    if (procLex == ct.lexeme)
                    {
                        Console.WriteLine("Error on line {0} when declaring constant {1}. Constants cannot have the same name as their enclosing procedure",
                            rec.symbol.lineNumber, ct.lexeme);
                        Console.ReadLine();
                        Environment.Exit(1);
                    }
                    Record pr = symTable.Lookup(procLex);
                    pr.procRecord.AddLocal(rec.constRecord);
                }

                ConstTail(procLex);
            }
            //ConstTail -> emptyt
        }

        private void VarPart(string procLex)
        {
            offset = 0;
           //VarPart->vart VarTail
           if (nt.type == Tokens.vart)
           {
              Match(Tokens.vart);
              VarTail(procLex);
           }
           //VarPart -> emptyt
        }

        private void VarTail(string procLex)
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
                    entry.varRecord = new VariableRecord(type, offset, varSize);
                    if(procLex != "")
                    {
                        if (procLex == id)
                        {
                            Crash(string.Format("Error on line {0} when declaring variable {1}. Variables cannot have the same name as their enclosing procedure",
                                entry.symbol.lineNumber, id));
                        }

                        Record pr = symTable.Lookup(procLex);
                        pr.procRecord.AddLocal(entry.varRecord);
                    }
                }
                VarTail(procLex);
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
            var procLex = ProcHeading();
            headingLexes.Push(procLex);
            Match(Tokens.semicolont);
            ProcBody(procLex);

            //popperino
            Match(Tokens.idt);
            PopHeadingLex(ct);

            Match(Tokens.semicolont);

            symTable.WriteTable(depth);
            symTable.DeleteDepth(depth);
            depth--;
        }

        private string ProcHeading()
        {
            //ProcHeading -> proceduret idt Args
            Match(Tokens.proceduret);
            Match(Tokens.idt);
            var rec = symTable.Insert(ct.lexeme, ct, depth);
            rec.procRecord = new ProcedureRecord();
            depth++;
            string procLex = ct.lexeme;
            Args(ct.lexeme);
            return procLex;
        }

        private void ProcBody(string procLex)
        {
            //ProcBody -> DeclarativePart StatementPart endt
            DeclarativePart(procLex);
            StatementPart();
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
                var rec = new VariableRecord(type, 0, size);
                procRec.procRecord.AddParameter(rec, hasVarMode);
                var vRec = symTable.Lookup(id);
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

        private void StatementPart()
        {
            //StatementPart -> begint SeqOfStatements
            if (nt.type == Tokens.begint)
            {
                Match(Tokens.begint);
                SeqOfStatements();
            }
            //StatementPart -> emptyt
        }

        private void SeqOfStatements()
        {
            //SeqOfStatements -> Statement ; StatTail | e
            if (nt.type == Tokens.idt)
            {
                Statement();
                Match(Tokens.semicolont);
                StatTail();
            }
        }

        private void StatTail()
        {
            //StatTail -> Statment ; StatTail | e
            if (nt.type == Tokens.idt)
            {
                Statement();
                Match(Tokens.semicolont);
                StatTail();
            }
        }

        private void Statement()
        {
            //Statement -> AssignStat | IOStat
            if (nt.type == Tokens.idt)
            {
                AssignStat();
            }
        }

        private void AssignStat()
        {
            //AssignStat -> idt := Expr
            Match(Tokens.idt);
            //Verify identifier is declared and is variable
            var sym = symTable.Lookup(ct.lexeme);
            if (sym == null)
            {
                Crash("Error on line {0}: Undeclared variable {1} used in assignment statment",
                    ct.lineNumber, ct.lexeme);
            }
            else if (sym.type != RecordTypes.VARIABLE)
            {
                Crash("Error on line {0}: identifier {1} is not of type VARIABLE",
                    ct.lineNumber, ct.lexeme);
            }

            Match(Tokens.assignopt);
            Expr();
        }

        private void IOStat()
        {
            //e
        }

        private void Expr()
        {
            //Expr -> Relation
            Relation();
        }

        private void Relation()
        {
            //Relation -> SimpleExpr
            SimpleExpr();
        }

        private void SimpleExpr()
        {
            //SimpleExpr -> Term MoreTerm
            Term();
            MoreTerm();
        }

        private void MoreTerm()
        {
            //MoreTerm -> Addop Term MoreTerm | e
            Peek();
            Addop();
            Term();
            MoreTerm();
        }

        private void Term()
        {
            //Term -> Factor MoreFactor
            Factor();
            MoreFactor();
        }

        private void MoreFactor()
        {
            //MoreFactor -> Mulop Factor MoreFactor | e
            if (Peek(Tokens.mulopt))
            {
                Mulop();
                Factor();
                MoreFactor();
            }
        }

        private void Factor()
        {
            //Factor -> idt | numt | ( Expr ) | ~ Factor | SignOp Factor
            switch (nt.type)
            {
                case Tokens.idt:
                    Match(Tokens.idt); break;
                case Tokens.numt:
                    Match(Tokens.numt); break;
                case Tokens.lparent:
                    Match(Tokens.lparent);
                    Expr();
                    Match(Tokens.rparent);
                    break;
                case Tokens.tildet:
                    Match(Tokens.tildet);
                    Factor();
                    break;
                case Tokens.minust:
                    SignOp();
                    Factor();
                    break;
                default:
                    Match(Tokens.idt, Tokens.numt, Tokens.lparent, Tokens.tildet, Tokens.minust);break;
            }
        }

        private void Addop()
        {
            //Addop -> + | - | OR
            //Tokens.addopt = + | OR
            Match(Tokens.addopt, Tokens.minust);
        }

        private void Mulop()
        {
            //Mulop -> * | / | DIV | MOD | &
            //Tokens.mulopt = DIV | MOD | * | / | &
            Match(Tokens.mulopt);
        }

        private void SignOp()
        {
            //SignOp -> -
            Match(Tokens.minust);
        }
    }
}
