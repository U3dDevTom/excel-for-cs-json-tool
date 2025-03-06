using System.Collections.Generic;

namespace ExcelToJsonCs
{
    class CsFieldConfigInfo
    {
        public static readonly Dictionary<string, int> SimpleTypeAry = new Dictionary<string, int>()
        {
            { "bool", 1 },
            { "short", 2 },
            { "uint", 3 },
            { "int", 4 },
            { "ulong", 5 },
            { "long", 6 },
            { "float", 7 },
            { "double", 8 },
            { "string", 9 }
        };


        public static int CompareSimpleType(string l, string r)
        {
            if (SimpleTypeAry.TryGetValue(l, out var lSome) && SimpleTypeAry.TryGetValue(r, out var rSome))
            {
                return lSome.CompareTo(rSome);
            }
            else
            {
                return 0;
            }
        }

        public string FieldName;
        public string FieldType;
        public bool C;
        public bool S;


        public CsFieldConfigInfo(string fieldName, string type, bool c, bool s)
        {
            this.FieldName = fieldName;
            this.FieldType = type;
            this.C = c;
            this.S = s;
        }


        public bool IsSystemTypes()
        {
            return SimpleTypeAry.ContainsKey(FieldType) ||
                   SimpleTypeAry.ContainsKey(this.FieldType.Substring(0, this.FieldType.Length - 2));
        }


        public void Update(CsFieldConfigInfo newField)
        {
            if (SimpleTypeAry.TryGetValue(this.FieldType, out var value))
            {
                if (SimpleTypeAry.TryGetValue(newField.FieldType, out var newValue))
                {
                    if (newValue > value)
                    {
                        this.FieldType = newField.FieldType;
                    }
                }
            }
            else if (SimpleTypeAry.TryGetValue(this.FieldType.Substring(0, this.FieldType.Length - 2),
                         out var tempValue))
            {
                if (SimpleTypeAry.TryGetValue(newField.FieldType.Substring(0, this.FieldType.Length - 2),
                        out var newValue))
                {
                    if (newValue > tempValue)
                    {
                        this.FieldType = newField.FieldType;
                    }
                }
            }
        }
    }
}