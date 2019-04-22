using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    class CodeGenerator
    {
        SymTable symTable;
        StreamReader file;
        StreamWriter outout;

        string words;
        Queue<string> currentLine = new Queue<string>();

        public CodeGenerator(SymTable symTable, StreamReader file, StreamWriter outout)
        {
            this.symTable = symTable;
            this.file = file;
            this.outout = outout;
        }

        void GetNextTACLine()
        {
            currentLine.Clear();
            words = file.ReadLine();
            if (words != null)
            {
                var wordf = words.Split(' ', '\t');
                foreach (var word in wordf) currentLine.Enqueue(word);
            }
        }

        void GenerateLine(string line, params string[] args)
        {
            outout.WriteLine(line, args);
        }

        public void Generate()
        {
            GetNextTACLine();
            GenerateIntro();
            //Procedures that aren't main
            while(currentLine.Peek().ToUpper() == "PROC")
            {
                var proc = currentLine.Dequeue();
                var procname = currentLine.Dequeue();
                GenerateProcedure(procname);
            }

            //START statement
            var start = currentLine.Dequeue();
            GenerateMain(currentLine.Dequeue());
        }

        void GenerateIntro()
        {
            GenerateLine(".model small");
            GenerateLine(".stack 100h");
            GenerateLine(".data");
            GenerateDataSegment();
            GenerateLine(".code");
            GenerateLine("include io.asm");
        }

        void GenerateDataSegment()
        {
            var integerRecords = new List<Record>();
            var stringRecords = new List<Record>();

            foreach( Record r in symTable.GetRecordsAtDepth(0))
            {
                if (r.type == RecordTypes.VARIABLE)
                {
                    integerRecords.Add(r);
                }
                else if (r.type == RecordTypes.LITERAL)
                {
                    stringRecords.Add(r);
                }
            }

            foreach (Record r in integerRecords)
            {
                GenerateLine("{0} DW ?", r.symbol.lexeme);
            }

            int s = 0;
            foreach (Record r in stringRecords)
            {
                GenerateLine("_S{0} DB \"{1}\",\"$\"", s.ToString(), r.symbol.lexeme);
                s++;
            }
        }

        void GenerateMain(string startProc)
        {
            GenerateLine("main\tPROC");
            GenerateLine("mov ax, @data");
            GenerateLine("mov ds, ax");
            GenerateLine("");
            GenerateLine("call {0}", startProc);
            GenerateLine("");
            GenerateLine("mov ah, 04ch");
            GenerateLine("int 21h");
            GenerateLine("main ENDP");
            GenerateLine("END main");
        }

        void GenerateProcedure(string procname)
        {
            Record procRec = symTable.Lookup(procname);
            int sizeOfLocals = procRec.procRecord.sizeOfLocal;
            int sizeOFParams = procRec.procRecord.sizeOfParameters;

            GenerateLine("{0}\tPROC", procname);
            GenerateLine("push bp");
            GenerateLine("mov bp, sp");
            GenerateLine("sub sp, {0}", sizeOfLocals.ToString());

            GenerateBody();

            GenerateLine("add sp, {0}", sizeOfLocals.ToString());
            GenerateLine("pop bp");
            GenerateLine("ret {0}", sizeOFParams.ToString());
            GenerateLine("{0}\tPROC", procname);
        }

        void GenerateBody()
        {
            GetNextTACLine();
            while (!words.ToUpper().Contains("ENDP "))
            {
                TranslateTACStatement();
                GetNextTACLine();
            }
                
        }

        void TranslateTACStatement()
        {
            if (words.Contains("="))
            {
                if (words.Contains(" * "))
                    TranslateTACMultiplicationAssignment();
                else if (words.Contains(" + "))
                    TranslateTACAdditionAssignment();
                else
                    TranslateTACSimpleAssignment();
            }
            else
            {
                GenerateLine(words);
            }

        }

        void TranslateTACSimpleAssignment()
        {
            var lside = TranslateOperand(currentLine.Dequeue());
            var equalsOperator = currentLine.Dequeue();
            var rside = TranslateOperand(currentLine.Dequeue());

            GenerateLine("mov AX,{0}", rside);
            GenerateLine("mov {0}, AX", lside);
        }

        void TranslateTACMultiplicationAssignment()
        {
            var lside = TranslateOperand(currentLine.Dequeue());
            var equalsOperator = currentLine.Dequeue();
            var lop = TranslateOperand(currentLine.Dequeue());
            var op = currentLine.Dequeue();
            var rop = currentLine.Dequeue();

            GenerateLine("mov AX, {0}", lop);
            GenerateLine("mov BX, {0}", rop);
            GenerateLine("imul BX");
            GenerateLine("mov {0}", lside);
        }

        void TranslateTACAdditionAssignment()
        {
            var lside = TranslateOperand(currentLine.Dequeue());
            var equalsOperator = currentLine.Dequeue();
            var lop = TranslateOperand(currentLine.Dequeue());
            var op = currentLine.Dequeue();
            var rop = currentLine.Dequeue();

            GenerateLine("mov AX, {0}", lop);
            GenerateLine("add AX, {0}", rop);
            GenerateLine("mov {0}, AX", lside);
        }

        string TranslateOperand(string operand)
        {
            string result = "";
            if (operand.StartsWith("@"))
            {
                operand = operand.TrimStart('@');
                result += "offset ";
            }

            if (operand.StartsWith("_bp"))
            {
                operand = operand.ToUpper();
                operand = "[" + operand + "]";
                operand.TrimStart('_');
            }
            
            result += operand;

            return result.ToUpper();
        }


    }
}
