#nullable enable
using System;
using System.Text;
using QuickFix.Fields;

namespace QuickFix.Message
{
    public class Header : FieldMap
    {
        public int[] HEADER_FIELD_ORDER = { Tags.BeginString, Tags.BodyLength, Tags.MsgType };

        public Header()
            : base(new ReusableFields.ReusableFieldsLengths(5, 5, 5, 5, 5, 5))
        { }

        public Header(Header src)
            : base(src)
        { }

        public override StringBuilder CalculateString(bool orderPostFieldOrder, StringBuilder sb)
        {
            var result = CalculateString(sb ?? new StringBuilder(64), HEADER_FIELD_ORDER, orderPostFieldOrder);
            return result;
        }

        public override StringBuilder CalculateString(StringBuilder sb, int[] preFields, bool orderPostFieldOrder)
        {
            return base.CalculateString(sb, HEADER_FIELD_ORDER, orderPostFieldOrder);
        }
    }
}
