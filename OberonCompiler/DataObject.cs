using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OberonCompiler
{
    interface IDataObject
    {
        VarTypes getType();
        string getTACString();
    }

    public enum VarTypes
    {
        charType,
        intType,
        floatType,
        stringType
    }

    public class StringLiteral : IDataObject
    {
        private static int numberOfStrings = 0;
        public StringLiteral(string value)
        {
            this.value = value;
            this.referenceValue = "_S" + numberOfStrings++.ToString();
        }

        public string referenceValue;
        public string value;
        public VarTypes type = VarTypes.stringType;

        public VarTypes getType() => type;
        public string getTACString() => value;
    }

    public class LiteralValue : IDataObject
    {
        public LiteralValue(VarTypes type, string value)
        {
            this.type = type;
            this.value = value;
        }

        public LiteralValue(string value)
        {
            this.value = value;
            type = value.Contains('.') ? VarTypes.floatType : VarTypes.intType;
        }

        public string value;
        public VarTypes type;

        public VarTypes getType() => type;
        public string getTACString() => value;
    }

    public class VariableRecord : IDataObject
    {
        public VariableRecord(Token t, VarTypes type, int offset, int size, int depth, bool isArgument, bool passByReference)
        {
            this.type = type;
            this.offset = offset;
            this.size = size;
            this.isArgument = isArgument;
            this.depth = depth;
            lexeme = t.lexeme;
            this.passByReference = passByReference;
        }

        public VarTypes type;
        public int offset;
        public int size;
        public int depth;
        public bool isArgument;
        public bool passByReference;
        public string lexeme;

        public string getTACString()
        {
            if (depth == 0)
            {
                return lexeme;
            }
            else
            {
                char op = isArgument ? '+' : '-';
                var s = passByReference ? "@" : "";
                return s + "_bp" + op + offset;
            }
        }

        public VarTypes getType() => type;
    }

    public class ConstantRecord : IDataObject
    {
        public ConstantRecord(Token s)
        {
            if (s.value != null)
            {
                this.type = VarTypes.intType;
                value = s.value;
            }
            else if (s.valueR != null)
            {
                this.type = VarTypes.floatType;
                valueR = s.valueR;
            }
        }
        public VarTypes type;
        public int? value;
        public double? valueR;

        public string getTACString()
        {
            if (value != null)
                return value.ToString();
            else if (valueR != null)
                return valueR.ToString();
            return "Error - No data for constant";
        }

        public VarTypes getType() => type;
    }
}
