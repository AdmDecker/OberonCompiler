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
        public void Goal(Analyzer a)
        {
            this.a = a;
            this.nt = a.getNextToken();
            Prog();
            Match(Tokens.eoft);
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
                    ct.lexeme, ct.lineNumber, s, ct.ToString());
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

        private void DeclarativePart()
        {
            //DeclarativePart -> ConstPart VarPart ProcPart
            ConstPart();
            VarPart();
            ProcPart();
        }

        private void ConstPart()
        {
            //ConstPart -> constt ConstTail
            if (nt.token == Tokens.modulet)
            {
                Match(Tokens.constt);
                ConstTail();
            }
            //ConstPart -> emptyt
        }

        private void ConstTail()
        {
            //ConstTail -> idt equalt Value semicolont ConstTail
            if (nt.token == Tokens.idt)
            {
                Match(Tokens.idt);
                Match(Tokens.equalt);
                Value();
                Match(Tokens.semicolont);
                ConstTail();
            }
            //ConstTail -> emptyt
        }

        private void VarPart()
        {
           //VarPart->vart VarTail
           if (nt.token == Tokens.vart)
           {
              Match(Tokens.vart);
              VarTail();
           }
           //VarPart -> emptyt
        }

        private void VarTail()
        {
            //VarTail -> IdentifierList colont TypeMark semicolont VarTail
            if (nt.token == Tokens.idt)
            {
                IdentifierList();
                Match(Tokens.colont);
                TypeMark();
                Match(Tokens.semicolont);
                VarTail();
            }
            //VarTail -> emptyt
        }

        private void IdentifierList()
        {
            //IdentifierList -> idt IdentifierList'
            Match(Tokens.idt);
            IdentifierList2();
        }

        private void IdentifierList2()
        {
            //IdentifierList' -> commat idt IdentifierList'
            if (nt.token == Tokens.commat)
            {
                Match(Tokens.commat);
                Match(Tokens.idt);
                IdentifierList2();
            }
            //IdentifierList' -> emptyt
        }

        private void TypeMark()
        {
            //TypeMark -> integert
            //TypeMark->realt
            //TypeMark->chart
            Match(Tokens.integert, Tokens.chart, Tokens.realt);
        }

        private void Value()
        {
            //Value -> numt
            Match(Tokens.numt);
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
            ProcHeading();
            Match(Tokens.semicolont);
            ProcBody();
            Match(Tokens.idt);
            Match(Tokens.semicolont);
        }

        private void ProcHeading()
        {
            //ProcHeading -> proceduret idt Args
            Match(Tokens.proceduret);
            Match(Tokens.idt);
            Args();
        }

        private void ProcBody()
        {
            //ProcBody -> DeclarativePart StatementPart endt
            DeclarativePart();
            StatementPart();
            Match(Tokens.endt);
        }

        private void Args()
        {
            //Args -> lparent ArgList rparent
            if(nt.token == Tokens.lparent)
            {
                Match(Tokens.lparent);
                ArgList();
                Match(Tokens.rparent);
            }
            //Args -> emptyt
        }

        private void ArgList()
        {
            // ArgList->Mode IdentifierList colont TypeMark MoreArgs
            Mode();
            IdentifierList();
            Match(Tokens.colont);
            TypeMark();
            MoreArgs();
        }

        private void MoreArgs()
        {
            //MoreArgs -> semicolont ArgList
            if (nt.token == Tokens.semicolont)
            {
                Match(Tokens.semicolont);
                ArgList();
            }
            //MoreArgs -> emptyt
        }

        private void Mode()
        {
            //Mode -> vart
            if (nt.token == Tokens.vart)
            {
                Match(Tokens.vart);
            }
            //Mode -> emptyt
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
