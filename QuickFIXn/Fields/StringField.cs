namespace QuickFix.Fields
{
    /// <summary>
    /// A string-valued message field
    /// </summary>
    public class StringField : FieldBase<string>
    {
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

        public override IField GetCopy()
        {
            return new StringField(Tag, Obj);
        }
    }
}
