using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    

    class SymTable
    {
        const int tableSize = 211;
        Record[] table = new Record[tableSize];

        //Insert(lex, token, depth) - insert the lexeme, token and depth into a record in the symbol table.
        public void Insert(string lex, Symbol token, int depth)
        {
            Record newRecord = new Record(token, depth);

            int hash = this.Hash(lex);
            if (table[hash] == null)
            {
                table[hash] = newRecord;
            }
            else
            {
                newRecord.nextNode = table[hash];
                table[hash] = newRecord;
            }
        }

        //Lookup(lex) - lookup uses the lexeme to find the entry and returns a pointer to that entry.
        public Record Lookup(string lex)
        {
            Record curRecord = table[Hash(lex)];

            while(curRecord != null && curRecord.symbol.lexeme != lex)
            {
                curRecord = curRecord.nextNode;
            }
            return curRecord;
        }

        //DeleteDepth(depth) - delete is passed the depth and deletes all records that are in the table at that depth.
        public void DeleteDepth(int depth)
        {
            for(int i = 0; i < tableSize; i++)
            {
                Record rec = table[i];
                if (rec == null)
                    continue;

                if (rec.nextNode == null)
                {
                    if (rec.depth == depth)
                        table[i] = null;
                }
                else
                {
                    DeleteLinked(depth, rec, rec.nextNode);
                    if (rec.depth == depth)
                        table[i] = rec.nextNode;
                }
            }
        }

        //Recursively delete members of a linked list at a target depth
        private void DeleteLinked(int depth, Record prev, Record current)
        {
            if (current.nextNode != null)
                DeleteLinked(depth, current, current.nextNode);

            if (current.depth == depth)
                prev.nextNode = current.nextNode;
        }

        //WriteTable(depth) - include a procedure that will write out all variables (lexeme only) that are in the table 
        //at a specified depth. [ this will be useful for debugging your compiler ]
        public void WriteTable(int depth)
        {
            Console.WriteLine("In-Table lexeme");
            for(int i = 0; i < tableSize; i++)
            {
                DisplayLinked(table[i], depth);
            }
        }

        //Display the contents of a linked list based on depth
        private void DisplayLinked(Record r, int depth)
        {
            if (r == null)
                return;

            if (r.depth == depth)
                Console.WriteLine(r.symbol.lexeme);

            DisplayLinked(r.nextNode, depth);
        }

        //hash(lexeme) - (private)passed a lexeme and return the location for that lexeme. 
        //(this should be an internal routine only, do not list in the interface section). 
        private int Hash(string lex)
        {
            char p;
            uint h = 0, g;
            for (int i = 0; i < lex.Length; i++)
            {
                p = lex[i];
                h = (h << 24) + p;
                if ((g = h & 0xf0000000) != 0)
                {
                    h = h ^ (g >> 24);
                    h = h ^ g;
                }
            }
            return (int)(h % tableSize);
        }
    }

    

    enum RecordTypes
    {
        VARIABLE,
        CONSTANT,
        PROCEDURE
    }

    class Record
    {
        public Record(Symbol token, int depth)
        {
            this.token = token.token;
            this.symbol = token;
            this.depth = depth;
        }

        public int depth;
        public Tokens token;
        public Symbol symbol;
        //PROPERTIES
        //Type of record (VARIABLE, CONSTANT, PROCEDURE)
        public RecordTypes type;

        public VariableRecord varRecord;
        public ConstantRecord constRecord;
        public ProcedureRecord procRecord;

        public Record nextNode;
    }

    enum VarTypes
    {
        charType,
        intType,
        floatType,
    }
    class VariableRecord {
        public VarTypes type;
        public int offset;
        public int size;
    }

    class ConstantRecord
    {
        public VarTypes type;
        public int offset;
        public int value;
        public float valueR;
    }

    class ProcedureRecord
    {
        public VarTypes returnType;
        public int sizeOfLocal;
        public int numberOfParameters;

        //Linked list of parameters
        public Parameter parameters;
    }

    class Parameter
    {
        public VarTypes paramType;
        public Parameter nextParameter;
    }
}
