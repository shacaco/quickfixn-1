#nullable enable
using System;
using QuickFix.Fields;

namespace QuickFix
{
    internal class MessageBuilder
    {
        private readonly DataDictionary.DataDictionary _sessionDict;
        private readonly DataDictionary.DataDictionary _appDict;
        private readonly IMessageFactory _msgFactory;
        private readonly Message.Message _reusableMessage;
        private Message.Message? _message;
        private readonly QuickFix.Fields.ApplVerID _defaultApplVerId;

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
            _sessionDict = sessionDD;
            _appDict = appDD;
            _msgFactory = msgFactory;
            _reusableMessage = new Message.Message();
        }

        internal Message.Message Build(ReadOnlySpan<char> msg, bool validateLengthAndChecksum)
        {
            MsgType = Message.Message.IdentifyType(msg, MsgType);
            BeginString = Message.Message.ExtractBeginString(msg, BeginString);
            _message = _reusableMessage.ClearAndInitialize(BeginString.Obj, MsgType.Obj);
            _message.FromString(msg, validateLengthAndChecksum, _sessionDict, _appDict, _msgFactory);
            return _message;
        }

        internal Message.Message RejectableMessage()
        {
            return _message;
        }
    }
}
