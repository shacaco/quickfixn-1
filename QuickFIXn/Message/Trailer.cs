#nullable enable
using System;
using System.Text;
using QuickFix.Fields;

namespace QuickFix.Message {
    public class Trailer : FieldMap 
    {
        public int[] TRAILER_FIELD_ORDER = { Tags.SignatureLength, Tags.Signature, Tags.CheckSum };

        public Trailer()
            : base(new ReusableFields.ReusableFieldsLengths(5, 5, 5, 5, 5, 5))
        { }

        public Trailer(Trailer src)
            : base(src)
        { }

        public override StringBuilder CalculateString(bool orderPostFieldOrder, StringBuilder sb)
        {
            var result = base.CalculateString(sb ?? new StringBuilder(64), TRAILER_FIELD_ORDER, orderPostFieldOrder);
            return result;
        }

        public override StringBuilder CalculateString(StringBuilder sb, int[] preFields, bool orderPostFieldOrder)
        {
            return base.CalculateString(sb, TRAILER_FIELD_ORDER, orderPostFieldOrder);
        }
    }
}
