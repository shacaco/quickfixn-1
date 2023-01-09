using My_Collections;
using System;

namespace QuickFix.Fields
{
    public class MemoryField : FieldBase<ReadOnlyMemory<char>>
    {
        public static readonly FactoryRepo<MemoryField> Factory = new FactoryRepo<MemoryField>(200000, () => new MemoryField(), 199900);

        private MemoryField()
            : base(-1, null) { }
        public MemoryField(int tag)
            : base(tag, null) { }

        public MemoryField(int tag, ReadOnlyMemory<char> val)
            : base(tag, val)
        {  }

        // quickfix compat
        public ReadOnlyMemory<char> getValue()
        { return Obj; }

        public void setValue(ReadOnlyMemory<char> val)
        { Obj = val; }

        protected override string makeString()
        {
            return Obj.ToString();
        }
    }
}
