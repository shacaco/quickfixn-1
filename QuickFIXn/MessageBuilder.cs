using QuickFix.Fields;

namespace QuickFix
{
    internal class MessageBuilder
    {
        private readonly DataDictionary.DataDictionary _sessionDD;
        private readonly DataDictionary.DataDictionary _appDD;
        private readonly IMessageFactory _msgFactory;
        private readonly QuickFix.Fields.ApplVerID _defaultApplVerId;

        private StringField _msgType;
        private string _beginString;
        private string _msgStr;
        private Message _message;

        public string OriginalString => _msgStr;
        public StringField MsgType => _msgType;

        /// <summary>
        /// The BeginString from the raw FIX message
        /// </summary>
        public string BeginString { get { return _beginString; } }

        internal MessageBuilder(string defaultApplVerId,
            DataDictionary.DataDictionary sessionDD,
            DataDictionary.DataDictionary appDD,
            IMessageFactory msgFactory)
        {
            _defaultApplVerId = new ApplVerID(defaultApplVerId);
            _sessionDD = sessionDD;
            _appDD = appDD;
            _msgFactory = msgFactory;
        }

        internal Message Build(bool validateLengthAndChecksum)
        {
            _message = _msgFactory.Create(_beginString, _defaultApplVerId, _msgType.Obj);
            _message.FromString(_msgStr, validateLengthAndChecksum, _sessionDD, _appDD, _msgFactory);
            return _message;
        }

        internal void SetData(string msgStr)
        {
            _msgStr = msgStr;
            _msgType = Message.IdentifyType(_msgStr);
            _beginString = Message.ExtractBeginString(_msgStr);
        }

        internal Message RejectableMessage()
        {
            if (_message != null)
                return _message;

            Message message = _msgFactory.Create(_beginString, _msgType.Obj);
            message.FromString(
                _msgStr,
                false,
                _sessionDD,
                _appDD,
                _msgFactory,
                true);
            return message;
        }
    }
}
