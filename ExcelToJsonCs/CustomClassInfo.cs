using System.Collections.Generic;

namespace ExcelToJsonCs
{
    sealed class CustomClassInfo
    {
        public string TypeName;


        public Dictionary<string, CsFieldConfigInfo> CustomFields = new Dictionary<string, CsFieldConfigInfo>();


        public void AddOrReplace(CsFieldConfigInfo value)
        {
            if (this.CustomFields.TryGetValue(value.FieldName, out var old))
            {
                old.Update(value);
            }
            else
            {
                this.CustomFields.Add(value.FieldName,value);
            }
        }
    }
}