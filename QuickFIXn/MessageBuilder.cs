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

        public string OriginalString { get; private set; } 

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

        internal Message Build(bool validateLengthAndChecksum)
        {
            _message = _reusableMessage.ClearAndInitialize(BeginString, MsgType);
            _message.FromString(OriginalString, validateLengthAndChecksum, _sessionDD, _appDD, _msgFactory, reusableFields);
            return _message;
        }

        internal void SetData(string msgStr)
        {
            OriginalString = msgStr;
            MsgType = Message.IdentifyType(msgStr, MsgType);
            BeginString = Message.ExtractBeginString(msgStr, BeginString);
            _message = null;
        }

        internal Message RejectableMessage()
        {
            if (_message != null)
                return _message;

            Message message = _msgFactory.Create(BeginString.Obj, MsgType.Obj);
            message.FromString(
                OriginalString,
                false,
                _sessionDD,
                _appDD,
                _msgFactory,
                true);
            return message;
        }
    }
}
