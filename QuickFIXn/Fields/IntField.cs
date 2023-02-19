namespace QuickFix.Fields
{
    /// <summary>
    /// An integer message field
    /// </summary>
    public class IntField : FieldBase<int>
    {
        private IntField()
            : base(-1, 0) { }

        public IntField(int tag)
            : base(tag, 0) { }

        public IntField(int tag, int val)
            : base(tag, val) {}

        // quickfix compat
        public int getValue()
        { return Obj; }

        public void setValue(int v)
        { Obj = v; }

        protected override string makeString()
        {
            return Converters.IntConverter.Convert(Obj);
        }

        public override IField GetCopy()
        {
            return new IntField(Tag, Obj);
        }
    }
}
