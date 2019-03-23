using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    //Recursive descent parser
    class RDParser
    {
        private Symbol ct; //Current token
        private Symbol nt; //Next token
        Analyzer a;
        SymTable symTable;
        int offset = 0;
        private int depth = 0;

        public void Goal(Analyzer a, SymTable s)
        {
            this.a = a;
            this.symTable = s;
            this.nt = a.getNextToken();
            Prog();
            symTable.WriteTable(0);
            Match(Tokens.eoft);
            Console.WriteLine("Parse completed successfully");
        }

        public void Match(params Tokens[] t)
        {
            CycleTokens();
            bool success = false;
            foreach( Tokens b in t)
            {
                if (ct.token == b)
                    success = true;
            }

            if (!success)
            {
                string s = "";
                foreach(Tokens f in t)
                {
                    s += f.ToString();
                }
                Console.WriteLine(
                    "Parse Error: Unexpected token '{0}' of type {3} on line {1}, expected token of type(s): {2}",
                    ct.lexeme, ct.lineNumber, s, ct.token.ToString());
                Console.ReadLine();
                Environment.Exit(1);
            }
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
            Match(Tokens.semicolont);
            DeclarativePart();
            StatementPart();
            Match(Tokens.endt);
            Match(Tokens.idt);
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
            if (nt.token == Tokens.constt)
            {
                Match(Tokens.constt);
                ConstTail(procLex);
            }
            //ConstPart -> emptyt
        }

        private void ConstTail(string procLex)
        {
            //ConstTail -> idt equalt Value semicolont ConstTail
            if (nt.token == Tokens.idt)
            {
                Match(Tokens.idt);
                var rec = symTable.Insert(ct.lexeme, ct, depth);
                Match(Tokens.equalt);
                var s = Value();
                rec.constRecord = new ConstantRecord(s);
                Match(Tokens.semicolont);
                if (procLex != "")
                {
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
           if (nt.token == Tokens.vart)
           {
              Match(Tokens.vart);
              VarTail(procLex);
           }
           //VarPart -> emptyt
        }

        private void VarTail(string procLex)
        {
            //VarTail -> IdentifierList colont TypeMark semicolont VarTail
            if (nt.token == Tokens.idt)
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
            if (nt.token == Tokens.commat)
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
            switch(ct.token)
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

        private Symbol Value()
        {
            //Value -> numt
            Match(Tokens.numt);
            return ct;
        }

        private void ProcPart()
        {
            //ProcPart->ProcedureDecl ProcPart
            if (nt.token == Tokens.proceduret)
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
            Match(Tokens.semicolont);
            ProcBody(procLex);

            //don't worry about this idt, it's after hte proc
            Match(Tokens.idt);
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
            if(nt.token == Tokens.lparent)
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
            if (nt.token == Tokens.semicolont)
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
            if (nt.token == Tokens.vart)
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
            if (nt.token == Tokens.begint)
            {
                Match(Tokens.begint);
                SeqOfStatements();
            }
            //StatementPart -> emptyt
        }

        private void SeqOfStatements()
        {
            //SeqOfStatements -> emptyt
        }
    }
}
