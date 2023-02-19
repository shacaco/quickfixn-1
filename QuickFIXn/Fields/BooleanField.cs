using System;

namespace QuickFix.Fields
{
    /// <summary>
    /// FIX BooleanField class
    /// </summary>
    public class BooleanField : FieldBase<Boolean>
    {
        private BooleanField()
            : base(-1, false) { }

        public BooleanField(int tag)
            : base(tag, false) { }
           
        public BooleanField(int tag, Boolean b)
            : base(tag, b) { }

        /// <summary>
        /// quickfix-cpp compat - returns base type
        /// </summary>
        /// <returns>Boolean object</returns>
        public Boolean getValue()
        { return Obj; }

        /// <summary>
        /// quickfix-cpp compat - set object
        /// </summary>
        public void setValue(Boolean b)
        { Obj = b; }

        protected override string makeString()
        {
            return Converters.BoolConverter.Convert(Obj);
        }

        public override IField GetCopy()
        {
            return new BooleanField(Tag, Obj);
        }
    }
}
