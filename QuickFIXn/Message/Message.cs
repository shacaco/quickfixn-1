#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using QuickFix.DataDictionary;
using QuickFix.Fields;
using DD = QuickFix.DataDictionary.DataDictionary;

namespace QuickFix.Message
{
    /// <summary>
    /// Represents a FIX message
    /// </summary>
    public class Message : FieldMap
    {
        private static readonly string MSG_TYPE_STRING = $"{SohChar}35=";
        public const char SohChar = '\u0001';
        public const string SohString = "\u0001";

        public const string Equal = "=";
        protected readonly StringBuilder _toStringBuilder = new StringBuilder(512);

        /// <summary>
        /// If message is invalid, then this is set to the tag that caused it
        /// </summary>
        private int _invalidField = 0;
        private bool _isValid = false;

        public Header Header { get; }
        public Trailer Trailer { get; }

        #region Constructors

        public Message() : base(
            new ReusableFields.ReusableFieldsLengths(30, 10, 10, 10, 10, 10))
        {
            Header = new Header();
            Trailer = new Trailer();
            _isValid = true;
        }

        public Message(string msgstr, bool validate = true)
            : this(msgstr, null, null, validate)
        { }

        public Message(string msgstr, DD dataDictionary, bool validate)
            : this()
        {
            FromString(msgstr, validate, dataDictionary, dataDictionary, null);
        }

        public Message(string msgstr, DD sessionDataDictionary, DD appDD, bool validate)
            : this()
        {
            FromStringHeader(msgstr);
            if (IsAdmin())
                FromString(msgstr, validate, sessionDataDictionary, appDD, null);
            else
                FromString(msgstr, validate, sessionDataDictionary, appDD, null, false);
        }

        public Message(Message src)
            : base(src)
        {
            Header = new Header(src.Header);
            Trailer = new Trailer(src.Trailer);
            _isValid = src._isValid;
            _invalidField = src._invalidField;
        }

        #endregion

        #region Static Methods

        public static bool IsAdminMsgType(string msgType)
        {
            return msgType.Length == 1 && "0A12345n".Contains(msgType[0]);
        }

        /// <summary>
        /// Parse the message type (tag 35) from a FIX string
        /// </summary>
        /// <param name="fixString">the FIX string to parse</param>
        /// <param name="reusableField"></param>
        /// <returns>the message type as a MsgType object</returns>
        /// <exception cref="MessageParseError">if 35 tag is missing or malformed</exception>
        public static StringField IdentifyType(ReadOnlySpan<char> fixString, StringField reusableField = null)
        {
            var f = reusableField ?? new StringField(-1);
            f.Set(MsgType.TAG, GetMsgType(fixString));
            return f;
        }

        public static int ExtractFieldTag(ReadOnlySpan<char> msg, int pos)
        {
            int tagend = msg.Slice(pos).IndexOf(Equal, StringComparison.Ordinal) + pos;
            int tag = int.Parse(msg.Slice(pos, tagend - pos));
            return tag;
        }

        public static StringField ExtractDataField(ReadOnlySpan<char> msg, int dataLength, ref int pos)
        {
            try
            {
                int tagend = msg.Slice(pos).IndexOf(Equal, StringComparison.Ordinal) + pos;
                int tag = int.Parse(msg.Slice(pos, tagend - pos));
                pos = tagend + 1;
                StringField field = new StringField(tag, msg.Slice(pos, dataLength).ToString());

                pos += dataLength + 1;
                return field;
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new MessageParseError($"Error at position ({pos}) while parsing msg ({msg})", e);
            }
            catch (OverflowException e)
            {
                throw new MessageParseError($"Error at position ({pos}) while parsing msg ({msg})", e);
            }
            catch (FormatException e)
            {
                throw new MessageParseError($"Error at position ({pos}) while parsing msg ({msg})", e);
            }
        }

        public static StringField ExtractField(ReadOnlySpan<char> msg, ref int pos, StringField? field)
        {
            try
            {
                int tagLength = msg.Slice(pos).IndexOf(Equal, StringComparison.Ordinal);
                int tag = int.Parse(msg.Slice(pos, tagLength));
                pos += tagLength + 1;
                int fieldValueLength = msg.Slice(pos).IndexOf(SohString, StringComparison.Ordinal);
                field ??= new StringField(-1);
                field.Set(tag, msg.Slice(pos, fieldValueLength).ToString());

                pos += fieldValueLength + 1;
                return field;
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new MessageParseError("Error at position (" + pos + ") while parsing msg (" + msg.ToString() + ")", e);
            }
            catch (OverflowException e)
            {
                throw new MessageParseError("Error at position (" + pos + ") while parsing msg (" + msg.ToString() + ")", e);
            }
            catch (FormatException e)
            {
                throw new MessageParseError("Error at position (" + pos + ") while parsing msg (" + msg.ToString() + ")", e);
            }
        }

        public static StringField ExtractField(ReadOnlySpan<char> msgstr, ref int pos)
        {
            return ExtractField(msgstr, ref pos, null);
        }

        public static StringField ExtractBeginString(ReadOnlySpan<char> msgstr, StringField reusableField = null)
        {
            int i = 0;
            return ExtractField(msgstr, ref i, reusableField);
        }

        public static bool IsHeaderField(int tag)
        {
            switch (tag)
            {
                case Tags.BeginString:
                case Tags.BodyLength:
                case Tags.MsgType:
                case Tags.SenderCompID:
                case Tags.TargetCompID:
                case Tags.OnBehalfOfCompID:
                case Tags.DeliverToCompID:
                case Tags.SecureDataLen:
                case Tags.MsgSeqNum:
                case Tags.SenderSubID:
                case Tags.SenderLocationID:
                case Tags.TargetSubID:
                case Tags.TargetLocationID:
                case Tags.OnBehalfOfSubID:
                case Tags.OnBehalfOfLocationID:
                case Tags.DeliverToSubID:
                case Tags.DeliverToLocationID:
                case Tags.PossDupFlag:
                case Tags.PossResend:
                case Tags.SendingTime:
                case Tags.OrigSendingTime:
                case Tags.XmlDataLen:
                case Tags.XmlData:
                case Tags.MessageEncoding:
                case Tags.LastMsgSeqNumProcessed:
                    // case Tags.OnBehalfOfSendingTime: TODO 
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsHeaderField(int tag, DD? dd)
        {
            if (IsHeaderField(tag))
                return true;
            if (dd is not null)
                return dd.IsHeaderField(tag);
            return false;
        }

        public static bool IsTrailerField(int tag)
        {
            switch (tag)
            {
                case Tags.SignatureLength:
                case Tags.Signature:
                case Tags.CheckSum:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsTrailerField(int tag, DD? dd)
        {
            if (IsTrailerField(tag))
                return true;
            if (dd is not null)
                return dd.IsTrailerField(tag);
            return false;
        }

        private static string GetFieldOrDefault(FieldMap fields, int tag, string defaultValue)
        {
            if (!fields.IsSetField(tag))
                return defaultValue;

            try
            {
                return fields.GetString(tag);
            }
            catch (FieldNotFoundException)
            {
                return defaultValue;
            }
        }

        private static SessionID GetReverseSessionId(Message msg)
        {
            return new SessionID(
                GetFieldOrDefault(msg.Header, Tags.BeginString, SessionID.NOT_SET),
                GetFieldOrDefault(msg.Header, Tags.TargetCompID, SessionID.NOT_SET),
                GetFieldOrDefault(msg.Header, Tags.TargetSubID, SessionID.NOT_SET),
                GetFieldOrDefault(msg.Header, Tags.TargetLocationID, SessionID.NOT_SET),
                GetFieldOrDefault(msg.Header, Tags.SenderCompID, SessionID.NOT_SET),
                GetFieldOrDefault(msg.Header, Tags.SenderSubID, SessionID.NOT_SET),
                GetFieldOrDefault(msg.Header, Tags.SenderLocationID, SessionID.NOT_SET)
            );
        }

        public static SessionID GetReverseSessionId(string msg)
        {
            Message m = new Message(msg, true);
            return GetReverseSessionId(m);
        }

        /// <summary>
        /// Parse the message type (tag 35) from a FIX string
        /// </summary>
        /// <param name="msg">the FIX string to parse</param>
        /// <returns>message type</returns>
        /// <exception cref="MessageParseError">if 35 tag is missing or malformed</exception>
        public static string GetMsgType(ReadOnlySpan<char> msg)
        {
            try
            {
                var msgTypeTagIndex = msg.IndexOf(MSG_TYPE_STRING, StringComparison.Ordinal);
                if (msgTypeTagIndex < 0)
                    throw new Exception();

                var objStartIndex = msgTypeTagIndex + MSG_TYPE_STRING.Length;
                var nextSOHWithin = msg.Slice(objStartIndex).IndexOf(SohString, StringComparison.Ordinal);
                if (nextSOHWithin < 0)
                    throw new Exception();

                var nextEquals = msg.Slice(objStartIndex).IndexOf(Equal, StringComparison.Ordinal);
                if (nextEquals > 0 && nextEquals < nextSOHWithin)
                    throw new Exception();
                return msg.Slice(objStartIndex, nextSOHWithin).ToString();
            }
            catch (Exception)
            {
                throw new MessageParseError("missing or malformed tag 35 in msg: " + msg.ToString());
            }
        }

        public static ApplVerID GetApplVerID(string beginString)
        {
            switch (beginString)
            {
                case FixValues.BeginString.FIX40:
                    return new ApplVerID(ApplVerID.FIX40);
                case FixValues.BeginString.FIX41:
                    return new ApplVerID(ApplVerID.FIX41);
                case FixValues.BeginString.FIX42:
                    return new ApplVerID(ApplVerID.FIX42);
                case FixValues.BeginString.FIX43:
                    return new ApplVerID(ApplVerID.FIX43);
                case FixValues.BeginString.FIX44:
                    return new ApplVerID(ApplVerID.FIX44);
                case FixValues.BeginString.FIX50:
                    return new ApplVerID(ApplVerID.FIX50);
                case FixValues.BeginString.FIX50SP1:
                    return new ApplVerID(ApplVerID.FIX50SP1);
                case FixValues.BeginString.FIX50SP2:
                    return new ApplVerID(ApplVerID.FIX50SP2);
                default:
                    throw new ArgumentException($"ApplVerID for {beginString} not supported");
            }
        }

        #endregion

        private void FromStringHeader(string msgstr)
        {
            Clear();

            int pos = 0;
            int count = 0;
            while (pos < msgstr.Length)
            {
                StringField f = ExtractField(msgstr, ref pos);

                if (count < 3 && Header.HEADER_FIELD_ORDER[count++] != f.Tag)
                    return;

                if (IsHeaderField(f.Tag))
                    Header.SetField(f, false);
                else
                    break;
            }
        }


        /// <summary>
        /// Creates a Message from a FIX string
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="validate"></param>
        /// <param name="sessionDD"></param>
        /// <param name="appDD"></param>
        public void FromString(ReadOnlySpan<char> msg, bool validate, DD sessionDD, DD appDD)
        {
            FromString(msg, validate, sessionDD, appDD, null);
        }

        /// <summary>
        /// Create a Message from a FIX string
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="validate"></param>
        /// <param name="sessionDD"></param>
        /// <param name="appDD"></param>
        /// <param name="msgFactory">If null, any groups will be constructed as generic Group objects</param>
        /// <param name="reusableFields"></param>
        public void FromString(ReadOnlySpan<char> msg, bool validate,
            DD sessionDD, DD appDD, IMessageFactory msgFactory)
        {
            FromString(msg, validate, sessionDD, appDD, msgFactory, false);
        }

        /// <summary>
        /// Creates a Message from a FIX string
        /// </summary>
        /// <param name="msgstr"></param>
        /// <param name="validate"></param>
        /// <param name="sessionDD"></param>
        /// <param name="appDD"></param>
        /// <param name="msgFactory">If null, any groups will be constructed as generic Group objects</param>
        /// <param name="ignoreBody">(default false) if true, ignores all non-header non-trailer fields.
        ///   Intended for callers that only need rejection-related information from the header.
        ///   </param>
        /// <param name="reusableFields"></param>
        public void FromString(ReadOnlySpan<char> msgstr, bool validate,
            DD sessionDD, DD appDD, IMessageFactory msgFactory,
            bool ignoreBody)
        {
            Clear();

            bool expectingHeader = true;
            bool expectingBody = true;
            int count = 0;
            int pos = 0;
            IFieldMapSpec? msgMap = null;
            while (pos < msgstr.Length)
            {
                StringField? f = null;

                int fieldTag = ExtractFieldTag(msgstr, pos);
                if (fieldTag == Tags.XmlData)
                {
                    if (IsHeaderField(Tags.XmlDataLen))
                        f = ExtractDataField(msgstr, Header.GetInt(Tags.XmlDataLen), ref pos);
                    else if (IsSetField(Tags.XmlDataLen))
                        f = ExtractDataField(msgstr, GetInt(Tags.XmlDataLen), ref pos);
                }

                f ??= ExtractField(msgstr, ref pos, ReusableFields.GetNextReusableStringField());

                if (validate && count < 3 && Header.HEADER_FIELD_ORDER[count++] != f.Tag)
                    throw new InvalidMessage("Header fields out of order");

                if (IsHeaderField(f.Tag, sessionDD))
                {
                    if (!expectingHeader)
                    {
                        if (0 == _invalidField)
                            _invalidField = f.Tag;
                        _isValid = false;
                    }

                    if (Tags.MsgType.Equals(f.Tag))
                    {
                        if (appDD != null)
                            if (appDD is not null)
                            {
                                msgMap = appDD.GetMapForMessage(f.ToString());
                            }
                    }

                    if (!Header.SetField(f, false))
                        Header.RepeatedTags.Add(f);

                    if (sessionDD is not null && sessionDD.Header.IsGroup(f.Tag))
                    {
                        pos = SetGroup(f, msgstr, pos, Header, sessionDD.Header.GetGroupSpec(f.Tag), msgFactory);
                    }
                }
                else if (IsTrailerField(f.Tag, sessionDD))
                {
                    expectingHeader = false;
                    expectingBody = false;
                    if (!Trailer.SetField(f, false))
                        Trailer.RepeatedTags.Add(f);

                    if (sessionDD is not null && sessionDD.Trailer.IsGroup(f.Tag))
                    {
                        pos = SetGroup(f, msgstr, pos, Trailer, sessionDD.Trailer.GetGroup(f.Tag), msgFactory);
                    }
                }
                else if (ignoreBody == false)
                {
                    if (!expectingBody)
                    {
                        if (0 == _invalidField)
                            _invalidField = f.Tag;
                        _isValid = false;
                    }

                    expectingHeader = false;
                    if (!SetField(f, false))
                    {
                        RepeatedTags.Add(f);
                    }

                    if (msgMap is not null && msgMap.IsGroup(f.Tag))
                    {
                        pos = SetGroup(f, msgstr, pos, this, msgMap.GetGroupSpec(f.Tag), msgFactory);
                    }
                }
            }

            if (validate)
            {
                Validate();
            }
        }

        /// <summary>
        /// Creates a Message from FIX JSON Encoding.
        /// See: https://github.com/FIXTradingCommunity/fix-json-encoding-spec
        /// </summary>
        /// <param name="json"></param>
        /// <param name="validate"></param>
        /// <param name="transportDict"></param>
        /// <param name="appDict"></param>
        /// <param name="msgFactory">If null, any groups will be constructed as generic Group objects</param>
        public void FromJson(string json, bool validate,
            DD transportDict,
            DD appDict,
            IMessageFactory? msgFactory)
        {
            Clear();

            using (JsonDocument document = JsonDocument.Parse(json))
            {
                string? beginString = document.RootElement.GetProperty("Header").GetProperty("BeginString").GetString();
                string? msgType = document.RootElement.GetProperty("Header").GetProperty("MsgType").GetString();

                if (beginString is null || msgType is null)
                {
                    throw new ArgumentException(
                        $"JSON message has invalid/missing beginString ({beginString}) and/or msgType ({msgType})");
                }

                IFieldMapSpec msgMap = appDict.GetMapForMessage(msgType);
                FromJson(document.RootElement.GetProperty("Header"), beginString, msgType, msgMap, msgFactory, transportDict, Header);
                FromJson(document.RootElement.GetProperty("Body"), beginString, msgType, msgMap, msgFactory, appDict, this);
                FromJson(document.RootElement.GetProperty("Trailer"), beginString, msgType, msgMap, msgFactory, transportDict, Trailer);
            }

            Header.SetField(new BodyLength(BodyLength()), true);
            Trailer.SetField(new CheckSum(Fields.Converters.CheckSumConverter.Convert(CheckSum())), true);

            if (validate)
            {
                Validate();
            }
        }

        protected void FromJson(JsonElement jsonElement,
            string beginString,
            string msgType,
            IFieldMapSpec msgMap,
            IMessageFactory? msgFactory,
            DD dataDict,
            FieldMap fieldMap)
        {
            foreach (JsonProperty field in jsonElement.EnumerateObject())
            {
                if (dataDict.FieldsByName.TryGetValue(field.Name, out DDField? ddField))
                {
                    if (msgMap is not null && msgMap.IsGroup(ddField.Tag) && JsonValueKind.Array == field.Value.ValueKind)
                    {
                        foreach (JsonElement jsonGrp in field.Value.EnumerateArray())
                        {
                            IGroupSpec grpSpec = msgMap.GetGroupSpec(ddField.Tag);

                            Group grp = msgFactory?.Create(beginString, msgType, ddField.Tag)
                                ?? new Group(ddField.Tag, grpSpec.Delim);
                            FromJson(jsonGrp, beginString, msgType, grpSpec, msgFactory, dataDict, grp);
                            fieldMap.AddGroup(grp);
                        }
                    }

                    if (JsonValueKind.Array != field.Value.ValueKind)
                    {
                        fieldMap.SetField(new StringField(ddField.Tag, field.Value.ToString()));
                    }
                }
                else
                {
                    // this may be a custom tag given by number instead of name
                    if (int.TryParse(field.Name, out int customTagNumber))
                    {
                        fieldMap.SetField(new StringField(customTagNumber, field.Value.ToString()));
                    }
                }
            }
        }

        /// <summary>
        /// Constructs a group and stores it in this Message object
        /// </summary>
        /// <param name="grpNoFld">the group's counter field</param>
        /// <param name="msgstr">full message string</param>
        /// <param name="pos">starting character position of group</param>
        /// <param name="fieldMap">full message as FieldMap</param>
        /// <param name="groupSpec">group definition structure from dd</param>
        /// <param name="msgFactory">if null, then this method will use the generic Group class constructor</param>
        /// <returns></returns>
        protected int SetGroup(StringField grpNoFld, ReadOnlySpan<char> msgstr, int pos, FieldMap fieldMap, IGroupSpec groupSpec,
            IMessageFactory? msgFactory)
        {
            int grpEntryDelimiterTag = groupSpec.Delim;
            int grpPos = pos;
            Group? grp = null; // the group entry being constructed

            while (pos < msgstr.Length)
            {
                grpPos = pos;
                StringField f = ExtractField(msgstr, ref pos, null);
                if (f.Tag == grpEntryDelimiterTag)
                {
                    // This is the start of a group entry.

                    if (grp is not null)
                    {
                        // We were already building an entry, so the delimiter means it's done.
                        fieldMap.AddGroup(grp, false);
                    }

                    // Create a new group!
                    grp = msgFactory?.Create(ExtractBeginString(msgstr).Obj, GetMsgType(msgstr), grpNoFld.Tag)
                          ?? new Group(grpNoFld.Tag, grpEntryDelimiterTag);
                }
                else if (!groupSpec.IsField(f.Tag))
                {
                    // This field is not in the group, thus the repeating group is done.
                    if (grp is not null)
                    {
                        fieldMap.AddGroup(grp, false);
                    }
                    return grpPos;
                }
                else if (groupSpec.IsField(f.Tag) && grp != null && grp.IsSetField(f.Tag))
                {
                    // Tag is appearing for the second time within a group element.
                    // Presumably the sender didn't set the delimiter (or their DD has a different delimiter).
                    throw new RepeatedTagWithoutGroupDelimiterTagException(grpNoFld.Tag, f.Tag);
                }

                if (grp is null)
                {
                    // This means we got into the group's fields without finding a delimiter tag.
                    throw new GroupDelimiterTagException(grpNoFld.Tag, grpEntryDelimiterTag);
                }

                // f is just a field in our group entry.  Add it and iterate again.
                grp.SetField(f);
                if (groupSpec.IsGroup(f.Tag))
                {
                    // f is a counter for a nested group.  Recurse!
                    pos = SetGroup(f, msgstr, pos, grp, groupSpec.GetGroupSpec(f.Tag), msgFactory);
                }
            }

            return grpPos;
        }

        /// <summary>
        /// Check if this message was deemed valid.
        /// </summary>
        /// <param name="problemField">If invalid, then this is set to the field that is the problem</param>
        /// <returns></returns>
        public bool HasValidStructure(out int problemField)
        {
            problemField = _isValid ? 0 : _invalidField;
            return _isValid;
        }

        public void Validate()
        {
            try
            {
                int receivedBodyLength = Header.GetInt(Tags.BodyLength);
                if (BodyLength() != receivedBodyLength)
                    throw new InvalidMessage("Expected BodyLength=" + BodyLength() + ", Received BodyLength=" + receivedBodyLength + ", Message.SeqNum=" + Header.GetInt(Tags.MsgSeqNum));

                int receivedCheckSum = Trailer.GetInt(Tags.CheckSum);
                if (CheckSum() != receivedCheckSum)
                    throw new InvalidMessage("Expected CheckSum=" + CheckSum() + ", Received CheckSum=" + receivedCheckSum + ", Message.SeqNum=" + Header.GetInt(Tags.MsgSeqNum));
            }
            catch (FieldNotFoundException e)
            {
                throw new InvalidMessage("BodyLength or CheckSum missing", e);
            }
            catch (FieldConvertError e)
            {
                throw new InvalidMessage("BodyLength or Checksum has wrong format", e);
            }
        }

        public void ReverseRoute(Header header)
        {
            // required routing tags
            Header.RemoveField(Tags.BeginString);
            Header.RemoveField(Tags.SenderCompID);
            Header.RemoveField(Tags.SenderSubID);
            Header.RemoveField(Tags.SenderLocationID);
            Header.RemoveField(Tags.TargetCompID);
            Header.RemoveField(Tags.TargetSubID);
            Header.RemoveField(Tags.TargetLocationID);

            if (header.IsSetField(Tags.BeginString))
            {
                string beginString = header.GetString(Tags.BeginString);
                if (beginString.Length > 0)
                    Header.SetField(new BeginString(beginString));

                Header.RemoveField(Tags.OnBehalfOfLocationID);
                Header.RemoveField(Tags.DeliverToLocationID);

                if (string.CompareOrdinal(beginString, "FIX.4.1") >= 0)
                {
                    if (header.IsSetField(Tags.OnBehalfOfLocationID))
                    {
                        string onBehalfOfLocationId = header.GetString(Tags.OnBehalfOfLocationID);
                        if (onBehalfOfLocationId.Length > 0)
                            Header.SetField(new DeliverToLocationID(onBehalfOfLocationId));
                    }

                    if (header.IsSetField(Tags.DeliverToLocationID))
                    {
                        string deliverToLocationId = header.GetString(Tags.DeliverToLocationID);
                        if (deliverToLocationId.Length > 0)
                            Header.SetField(new OnBehalfOfLocationID(deliverToLocationId));
                    }
                }
            }

            if (header.IsSetField(Tags.SenderCompID))
            {
                SenderCompID senderCompId = new SenderCompID();
                header.GetField(senderCompId);
                if (senderCompId.Obj.Length > 0)
                    Header.SetField(new TargetCompID(senderCompId.Obj));
            }

            if (header.IsSetField(Tags.SenderSubID))
            {
                SenderSubID senderSubId = new SenderSubID();
                header.GetField(senderSubId);
                if (senderSubId.Obj.Length > 0)
                    Header.SetField(new TargetSubID(senderSubId.Obj));
            }

            if (header.IsSetField(Tags.SenderLocationID))
            {
                SenderLocationID senderLocationId = new SenderLocationID();
                header.GetField(senderLocationId);
                if (senderLocationId.Obj.Length > 0)
                    Header.SetField(new TargetLocationID(senderLocationId.Obj));
            }

            if (header.IsSetField(Tags.TargetCompID))
            {
                TargetCompID targetCompId = new TargetCompID();
                header.GetField(targetCompId);
                if (targetCompId.Obj.Length > 0)
                    Header.SetField(new SenderCompID(targetCompId.Obj));
            }

            if (header.IsSetField(Tags.TargetSubID))
            {
                TargetSubID targetSubId = new TargetSubID();
                header.GetField(targetSubId);
                if (targetSubId.Obj.Length > 0)
                    Header.SetField(new SenderSubID(targetSubId.Obj));
            }

            if (header.IsSetField(Tags.TargetLocationID))
            {
                TargetLocationID targetLocationId = new TargetLocationID();
                header.GetField(targetLocationId);
                if (targetLocationId.Obj.Length > 0)
                    Header.SetField(new SenderLocationID(targetLocationId.Obj));
            }

            // optional routing tags
            Header.RemoveField(Tags.OnBehalfOfCompID);
            Header.RemoveField(Tags.OnBehalfOfSubID);
            Header.RemoveField(Tags.DeliverToCompID);
            Header.RemoveField(Tags.DeliverToSubID);

            if (header.IsSetField(Tags.OnBehalfOfCompID))
            {
                string onBehalfOfCompID = header.GetString(Tags.OnBehalfOfCompID);
                if (onBehalfOfCompID.Length > 0)
                    Header.SetField(new DeliverToCompID(onBehalfOfCompID));
            }

            if (header.IsSetField(Tags.OnBehalfOfSubID))
            {
                string onBehalfOfSubID = header.GetString(Tags.OnBehalfOfSubID);
                if (onBehalfOfSubID.Length > 0)
                    Header.SetField(new DeliverToSubID(onBehalfOfSubID));
            }

            if (header.IsSetField(Tags.DeliverToCompID))
            {
                string deliverToCompID = header.GetString(Tags.DeliverToCompID);
                if (deliverToCompID.Length > 0)
                    Header.SetField(new OnBehalfOfCompID(deliverToCompID));
            }

            if (header.IsSetField(Tags.DeliverToSubID))
            {
                string deliverToSubID = header.GetString(Tags.DeliverToSubID);
                if (deliverToSubID.Length > 0)
                    Header.SetField(new OnBehalfOfSubID(deliverToSubID));
            }
        }

        public int CheckSum()
        {
            return
                (Header.CalculateTotal()
                + CalculateTotal()
                + Trailer.CalculateTotal()) % 256;
        }

        public bool IsAdmin()
        {
            return Header.IsSetField(Tags.MsgType) && IsAdminMsgType(Header.GetString(Tags.MsgType));
        }

        public bool IsApp()
        {
            return Header.IsSetField(Tags.MsgType) && !IsAdminMsgType(Header.GetString(Tags.MsgType));
        }

        /// <summary>
        /// FIXME less operator new
        /// </summary>
        /// <param name="sessionId"></param>
        public void SetSessionID(SessionID sessionId)
        {
            Header.SetField(new BeginString(sessionId.BeginString));
            Header.SetField(new SenderCompID(sessionId.SenderCompID));
            if (SessionID.IsSet(sessionId.SenderSubID))
                Header.SetField(new SenderSubID(sessionId.SenderSubID));
            if (SessionID.IsSet(sessionId.SenderLocationID))
                Header.SetField(new SenderLocationID(sessionId.SenderLocationID));
            Header.SetField(new TargetCompID(sessionId.TargetCompID));
            if (SessionID.IsSet(sessionId.TargetSubID))
                Header.SetField(new TargetSubID(sessionId.TargetSubID));
            if (SessionID.IsSet(sessionId.TargetLocationID))
                Header.SetField(new TargetLocationID(sessionId.TargetLocationID));
        }

        public SessionID GetSessionID(Message m)
        {
            bool isSetSenderSubId = m.Header.IsSetField(Tags.SenderSubID);
            bool isSetSenderLocationId = m.Header.IsSetField(Tags.SenderLocationID);
            bool isSetTargetSubId = m.Header.IsSetField(Tags.TargetSubID);
            bool isSetTargetLocationId = m.Header.IsSetField(Tags.TargetLocationID);

            if (isSetSenderSubId && isSetSenderLocationId && isSetTargetSubId && isSetTargetLocationId)
                return new SessionID(m.Header.GetString(Tags.BeginString),
                    m.Header.GetString(Tags.SenderCompID), m.Header.GetString(Tags.SenderSubID), m.Header.GetString(Tags.SenderLocationID),
                    m.Header.GetString(Tags.TargetCompID), m.Header.GetString(Tags.TargetSubID), m.Header.GetString(Tags.TargetLocationID));

            if (isSetSenderSubId && isSetTargetSubId)
                return new SessionID(m.Header.GetString(Tags.BeginString),
                    m.Header.GetString(Tags.SenderCompID), m.Header.GetString(Tags.SenderSubID),
                    m.Header.GetString(Tags.TargetCompID), m.Header.GetString(Tags.TargetSubID));

            return new SessionID(
                m.Header.GetString(Tags.BeginString),
                m.Header.GetString(Tags.SenderCompID),
                m.Header.GetString(Tags.TargetCompID));
        }

        public Message ClearAndInitialize()
        {
            return ClearAndInitialize(Header.GetString(Tags.BeginString), Header.GetString(Tags.MsgType));
        }

        internal Message ClearAndInitialize(string beginString, string msgType)
        {
            Clear();

            Header.SetWithReusableField(Tags.BeginString, beginString);
            Header.SetWithReusableField(Tags.MsgType, msgType);

            return this;
        }

        public override void Clear()
        {
            _invalidField = 0;
            Header.Clear();
            base.Clear();
            Trailer.Clear();
        }

        private object lock_ToString = new object();
        public override string ToString()
        {
            return ToString(false);
        }

        public int ToCharArray(bool orderBodyPostFieldOrder, char[] chars)
        {
            lock (lock_ToString)
            {
                var b = ToStringBuilder(orderBodyPostFieldOrder);
                b.CopyTo(0, chars, b.Length);
                return b.Length;
            }
        }

        public string ToString(bool orderBodyPostFieldOrder)
        {
            lock (lock_ToString)
            {
                return ToStringBuilder(orderBodyPostFieldOrder).ToString();
            }
        }

        private IntField _bodyLength = new IntField(Tags.BodyLength);
        private StringField _checkSum = new StringField(Tags.CheckSum);

        public StringBuilder ToStringBuilder(bool orderBodyPostFieldOrder)
        {
            lock (lock_ToString)
            {
                _bodyLength.setValue(BodyLength());
                Header.SetField(_bodyLength);
                _checkSum.setValue(Fields.Converters.CheckSumConverter.Convert(CheckSum()));
                Trailer.SetField(_checkSum);
                _toStringBuilder.Clear();
                Header.CalculateString(orderBodyPostFieldOrder, _toStringBuilder);
                CalculateString(orderBodyPostFieldOrder, _toStringBuilder);
                Trailer.CalculateString(orderBodyPostFieldOrder, _toStringBuilder);
                return _toStringBuilder;
            }
        }

        protected int BodyLength()
        {
            return Header.CalculateLength() + CalculateLength() + Trailer.CalculateLength();
        }

        private static string FieldMapToXML(DD? dd, FieldMap fields, int space)
        {
            StringBuilder s = new StringBuilder();

            // fields
            foreach (var f in fields.OrderBy(i => i.Key))
            {
                s.Append("<field ");
                if (dd is not null && dd.FieldsByTag.TryGetValue(f.Key, out var value))
                {
                    s.Append("name=\"" + value.Name + "\" ");
                }
                s.Append("number=\"" + f.Key + "\">");
                s.Append("<![CDATA[" + f.Value + "]]>");
                s.Append("</field>");
            }
            // now groups
            List<int> groupTags = fields.GetGroupTags();
            foreach (int groupTag in groupTags)
            {
                for (int counter = 1; counter <= fields.GroupCount(groupTag); counter++)
                {
                    s.Append("<group>");
                    s.Append(FieldMapToXML(dd, fields.GetGroup(counter, groupTag), space + 1));
                    s.Append("</group>");
                }
            }

            return s.ToString();
        }


        /// <summary>
        /// ToJSON() helper method.
        /// </summary>
        /// <returns>an XML string</returns>
        private static StringBuilder FieldMapToJSON(StringBuilder sb, DD? dd, FieldMap fields, bool humanReadableValues)
        {
            IList<int> numInGroupTagList = fields.GetGroupTags();
            IList<IField> numInGroupFieldList = new List<IField>();

            // Non-Group Fields
            foreach (var (_, field) in fields.OrderBy(f=>f.Key))
            {
                if (Fields.CheckSum.TAG == field.Tag)
                    continue; // FIX JSON Encoding does not include CheckSum

                if (numInGroupTagList.Contains(field.Tag))
                {
                    numInGroupFieldList.Add(field);
                    continue; // Groups will be handled below
                }

                if (dd is not null && dd.FieldsByTag.ContainsKey(field.Tag))
                {
                    sb.Append("\"" + dd.FieldsByTag[field.Tag].Name + "\":");
                    if (humanReadableValues)
                    {
                        if (dd.FieldsByTag[field.Tag].EnumDict.TryGetValue(field.ToString(), out var valueDescription))
                        {
                            sb.Append("\"" + valueDescription + "\",");
                        }
                        else
                            sb.Append("\"" + field + "\",");
                    }
                    else
                    {
                        sb.Append("\"" + field + "\",");
                    }
                }
                else
                {
                    sb.Append("\"" + field.Tag + "\":");
                    sb.Append("\"" + field + "\",");
                }
            }

            // Group Fields
            foreach (IField numInGroupField in numInGroupFieldList)
            {
                // The name of the NumInGroup field is the key of the JSON list containing the Group items
                if (dd is not null && dd.FieldsByTag.ContainsKey(numInGroupField.Tag))
                    sb.Append("\"" + dd.FieldsByTag[numInGroupField.Tag].Name + "\":[");
                else
                    sb.Append("\"" + numInGroupField.Tag + "\":[");

                // Populate the JSON list with the Group items
                for (int counter = 1; counter <= fields.GroupCount(numInGroupField.Tag); counter++)
                {
                    sb.Append("{");
                    FieldMapToJSON(sb, dd, fields.GetGroup(counter, numInGroupField.Tag), humanReadableValues);
                    sb.Append("},");
                }

                // Remove trailing comma
                if (sb.Length > 0 && sb[^1] == ',')
                    sb.Remove(sb.Length - 1, 1);

                sb.Append("],");
            }
            // Remove trailing comma
            if (sb.Length > 0 && sb[^1] == ',')
                sb.Remove(sb.Length - 1, 1);

            return sb;
        }

        /// <summary>
        /// Get a representation of the message as an XML string.
        /// (NOTE: this is just an ad-hoc XML; it is NOT FIXML.)
        /// </summary>
        /// <param name="dataDictionary">if null, then field names cannot and will not be in the output</param>
        /// <returns>an XML string</returns>
        public string ToXML(DD? dataDictionary = null)
        {
            StringBuilder s = new StringBuilder();
            s.Append("<message>");
            s.Append("<header>");
            s.Append(FieldMapToXML(dataDictionary, Header, 4));
            s.Append("</header>");
            s.Append("<body>");
            s.Append(FieldMapToXML(dataDictionary, this, 4));
            s.Append("</body>");
            s.Append("<trailer>");
            s.Append(FieldMapToXML(dataDictionary, Trailer, 4));
            s.Append("</trailer>");
            s.Append("</message>");
            return s.ToString();
        }

        /// <summary>
        /// Get a representation of the message as a string in FIX JSON Encoding.
        /// See: https://github.com/FIXTradingCommunity/fix-json-encoding-spec
        ///
        /// Per the FIX JSON Encoding spec, tags are converted to human-readable form, but values are not.
        /// </summary>
        /// <param name="dataDictionary">Needed if you want tag names emitted or humanReadableValues to work</param>
        /// <param name="humanReadableValues">
        ///   True will cause enums to be converted to human strings.
        ///   Will not (and cannot!) work if dataDictionary is null.
        /// </param>
        /// <returns>a JSON string</returns>
        public string ToJSON(DD? dataDictionary = null, bool humanReadableValues = false)
        {
            StringBuilder sb = new StringBuilder().Append("{").Append("\"Header\":{");
            FieldMapToJSON(sb, dataDictionary, Header, humanReadableValues).Append("},\"Body\":{");
            FieldMapToJSON(sb, dataDictionary, this, humanReadableValues).Append("},\"Trailer\":{");
            FieldMapToJSON(sb, dataDictionary, Trailer, humanReadableValues).Append("}}");
            return sb.ToString();
        }
    }
}
