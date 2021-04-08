using System;
using System.Collections.Generic;
using System.Text;
using My_Collections;

namespace QuickFix.Fields
{
    /// <summary>
    /// A string-valued message field
    /// </summary>
    public class StringField : FieldBase<string>
    {
        public static readonly FactoryRepo<StringField> Factory = new FactoryRepo<StringField>(2000000, ()=> new StringField(), 1999000);

        private StringField()
            : base(-1, "") { }
        public StringField(int tag)
            : base(tag, "") { }

        public StringField(int tag, string str)
            : base(tag, str) { }

        // quickfix compat
        public string getValue()
        { return Obj; }

        public void setValue(string val)
        { Obj = val; }

        protected override string makeString()
        {
            return Obj;
        }
    }
}
