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
        bool debugFlag = true;

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
                foreach (var word in wordf)
                {
                    if( word != "")
                        currentLine.Enqueue(word);
                }
            }
        }

        void GenerateLine(string line, params string[] args)
        {
            GenerateLineWithHeader(line, "", args);
        }

        void GenerateLineWithHeader(string line, string header, params string[] args)
        {
            string head = string.Format("{0, -6}", header);
            outout.WriteLine(head + line, args);
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
                if (r.symbol.type == Tokens.stringt)
                {
                    stringRecords.Add(r);
                }
                else if (r.type == RecordTypes.VARIABLE)
                {
                    integerRecords.Add(r);
                }
            }

            foreach (Record r in integerRecords)
            {
                GenerateLineWithHeader("DW ?", r.symbol.lexeme);
            }

            foreach (Record r in stringRecords)
            {
                GenerateLineWithHeader("DB \"{0}\",\"$\"", r.stringRecord.referenceValue, r.symbol.lexeme);
            }
        }

        void GenerateMain(string startProc)
        {
            string header = "main";
            GenerateLineWithHeader("PROC", header);
            GenerateLine("mov ax, @data");
            GenerateLine("mov ds, ax");
            GenerateLine("");
            GenerateLine("call {0}", startProc);
            GenerateLine("");
            GenerateLine("mov ah, 04ch");
            GenerateLine("int 21h");
            GenerateLineWithHeader("ENDP", header);
            GenerateLine("");
            GenerateLine("END {0}", header);
        }

        void GenerateProcedure(string procname)
        {
            Record procRec = symTable.Lookup(procname);
            int sizeOfLocals = procRec.procRecord.sizeOfLocal;
            int sizeOFParams = procRec.procRecord.sizeOfParameters;

            GenerateLineWithHeader("PROC", procname);
            GenerateLine("push bp");
            GenerateLine("mov bp, sp");
            GenerateLine("sub sp, {0}", sizeOfLocals.ToString());

            GenerateBody();
            GenerateLine("");

            GenerateLine("add sp, {0}", sizeOfLocals.ToString());
            GenerateLine("pop bp");
            GenerateLine("ret {0}", sizeOFParams.ToString());
            GenerateLineWithHeader("ENDP", procname);
        }

        void GenerateBody()
        {
            GetNextTACLine();
            while (!words.ToUpper().Contains("ENDP "))
            {
                TranslateTACStatement();
                GetNextTACLine();
            }
            //Throw away the ENDP
            GetNextTACLine();
        }

        void TranslateTACStatement()
        {
            GenerateLine("");
            if (debugFlag) GenerateLine(";" + words);
            if (words.Contains("="))
            {
                if (words.Contains(" * "))
                    TranslateTACMultiplicationAssignment();
                else if (words.Contains(" + "))
                    TranslateTACAdditionAssignment();
                else
                    TranslateTACSimpleAssignment();
            }
            else if (words.ToUpper().StartsWith("RD")) {
                TranslateInputStatement();
            }
            else if (words.ToUpper().StartsWith("WR"))
            {
                TranslateOutputStatement();
            }
            else if (currentLine.Peek().ToUpper() == "PUSH" || currentLine.Peek().ToUpper() == "POP")
            {
                var push = currentLine.Dequeue();
                var op = TranslateOperand(currentLine.Dequeue() ?? "");
                GenerateLine("{0} {1}", push, op);
            }
            else
            {
                GenerateLine(words);
            }
        }

        void TranslateInputStatement()
        {
            var op = currentLine.Dequeue();
            var variable = TranslateOperand(currentLine.Dequeue());

            GenerateLine("call readint");
            GenerateLine("mov {0}, bx", variable);
        }

        void TranslateOutputStatement()
        {
            var op = currentLine.Dequeue();

            if (op.ToUpper() == "WRS")
            {
                var variable = currentLine.Dequeue();
                GenerateLine("mov DX, OFFSET {0}", variable); //strings are always global so don't need to TranslateOperand()
                GenerateLine("call writestr");
            }
            else if (op.ToUpper() == "WRI")
            {
                var variable = TranslateOperand(currentLine.Dequeue());
                GenerateLine("mov AX, {0}", variable);
                GenerateLine("call writeint");
            }
            else if (op.ToUpper() == "WRLN")
            {
                GenerateLine("call writeln");
            }
            else GenerateLine("ERROR - UNKNOWN WRITE OPERATION");
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
            var rop = TranslateOperand(currentLine.Dequeue());

            GenerateLine("mov AX, {0}", lop);
            GenerateLine("mov BX, {0}", rop);
            GenerateLine("imul BX");
            GenerateLine("mov {0}, AX", lside);
        }

        void TranslateTACAdditionAssignment()
        {
            var lside = TranslateOperand(currentLine.Dequeue());
            var equalsOperator = currentLine.Dequeue();
            var lop = TranslateOperand(currentLine.Dequeue());
            var op = currentLine.Dequeue();
            var rop = TranslateOperand(currentLine.Dequeue());

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
                result += "OFFSET ";
            }

            if (operand.StartsWith("_bp"))
            {
                operand = operand.TrimStart('_');
                operand = operand.ToUpper();
                operand = "[" + operand + "]";
            }
            
            result += operand;

            return result;
        }
    }
}
