using System;

namespace QuickFix.Fields
{
    /// <summary>
    /// A decimal FIX field
    /// </summary>
    public class DecimalField : FieldBase<Decimal>
    {
        private DecimalField()
            : base(-1, new Decimal(0.0)) { }

        public DecimalField(int tag)
            : base(tag, new Decimal(0.0)) {}

        public DecimalField(int tag, Decimal val)
            : base(tag, val) { }

        // quickfix compat
        public Decimal getValue()
        { return Obj; }

        public void setValue(Decimal d)
        { Obj = d; }

        protected override string makeString()
        {
            return Converters.DecimalConverter.Convert(Obj);
        }

        public override IField GetCopy()
        {
            return new DecimalField(Tag, Obj);
        }
    }
}
