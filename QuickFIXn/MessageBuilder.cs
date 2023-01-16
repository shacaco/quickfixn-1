using System;
using QuickFix.Fields;
using System.Linq;

namespace QuickFix
{
    internal class MessageBuilder
    {
        private readonly DataDictionary.DataDictionary _sessionDD;
        private readonly DataDictionary.DataDictionary _appDD;
        private readonly QuickFix.Fields.ApplVerID _defaultApplVerId;
        private readonly IMessageFactory _msgFactory;
        private readonly StringField[] reusableFields = new StringField[100].Select(i => new StringField(-1)).ToArray();
        private readonly Message _reusableMessage = new Message();
        private Message _message;

        public StringField MsgType { get; private set; } = new StringField(-1);

        /// <summary>
        /// The BeginString from the raw FIX message
        /// </summary>
        public StringField BeginString { get; private set; } = new StringField(-1);

        internal MessageBuilder(string defaultApplVerId,
            DataDictionary.DataDictionary sessionDD,
            DataDictionary.DataDictionary appDD, IMessageFactory msgFactory)
        {
            _defaultApplVerId = new ApplVerID(defaultApplVerId);
            _sessionDD = sessionDD;
            _appDD = appDD;
            _msgFactory = msgFactory;
        }

        internal Message Build(ReadOnlySpan<char> msg, bool validateLengthAndChecksum)
        {
            MsgType = Message.IdentifyType(msg, MsgType);
            BeginString = Message.ExtractBeginString(msg, BeginString);
            _message = _reusableMessage.ClearAndInitialize(BeginString, MsgType);
            _message.FromString(msg, validateLengthAndChecksum, _sessionDD, _appDD, _msgFactory, reusableFields);
            return _message;
        }

        internal Message RejectableMessage()
        {
            return _message;
        }
    }
}
