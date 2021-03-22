using System;
using System.Collections.Generic;
using System.Text;
using My_Collections;

namespace QuickFix.Fields
{
    /// <summary>
    /// An integer message field
    /// </summary>
    public class IntField : FieldBase<int>
    {
        public static readonly FactoryRepo<IntField> Factory = new FactoryRepo<IntField>(500000, () => new IntField(), 499000);

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
    }
}
