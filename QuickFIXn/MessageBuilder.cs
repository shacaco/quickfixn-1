using System;
using QuickFix.Fields;

namespace QuickFix
{
    internal class MessageBuilder
    {
        private readonly DataDictionary.DataDictionary _sessionDD;
        private readonly DataDictionary.DataDictionary _appDD;
        private readonly QuickFix.Fields.ApplVerID _defaultApplVerId;
        private readonly IMessageFactory _msgFactory;
        private readonly Message _reusableMessage;
        private Message _message;

        public StringField MsgType { get; private set; } = new(-1);

        /// <summary>
        /// The BeginString from the raw FIX message
        /// </summary>
        public StringField BeginString { get; private set; } = new(-1);

        internal MessageBuilder(string defaultApplVerId,
            DataDictionary.DataDictionary sessionDD,
            DataDictionary.DataDictionary appDD, IMessageFactory msgFactory)
        {
            _defaultApplVerId = new ApplVerID(defaultApplVerId);
            _sessionDD = sessionDD;
            _appDD = appDD;
            _msgFactory = msgFactory;
            _reusableMessage = new Message();
            _reusableMessage.InitializeReusableFields(100);
        }

        internal Message Build(ReadOnlySpan<char> msg, bool validateLengthAndChecksum)
        {
            MsgType = Message.IdentifyType(msg, MsgType);
            BeginString = Message.ExtractBeginString(msg, BeginString);
            _message = _reusableMessage.ClearAndInitialize(BeginString.Obj, MsgType.Obj);
            _message.FromString(msg, validateLengthAndChecksum, _sessionDD, _appDD, _msgFactory);
            return _message;
        }

        internal Message RejectableMessage()
        {
            return _message;
        }
    }
}
