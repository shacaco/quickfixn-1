using System;
using System.Collections.Generic;

namespace QuickFix
{
    /// <summary>
    /// In-memory message store implementation
    /// </summary>
    public class MemoryStore : IMessageStore
    {
        #region Private Members

        internal System.Collections.Generic.Dictionary<int, string> Messages { get; private set; }
        private DateTime? _creationTime;

        #endregion

        public MemoryStore()
        {
            Messages = new System.Collections.Generic.Dictionary<int, string>();
            Reset();
        }

        public void Get(int begSeqNo, int endSeqNo, List<string> messages)
        {
            for (int current = begSeqNo; current <= endSeqNo; current++)
            {
                if (Messages.ContainsKey(current))
                    messages.Add(Messages[current]);
            }
        }

        #region MessageStore Members

        public bool Set(int msgSeqNum, ReadOnlySpan<char> msg)
        {
            Messages[msgSeqNum] = msg.ToString();
            return true;
        }

        public int NextSenderMsgSeqNum { get; set; }
        public int NextTargetMsgSeqNum { get; set; }

        public void IncrNextSenderMsgSeqNum()
        { ++NextSenderMsgSeqNum; }

        public void IncrNextTargetMsgSeqNum()
        { ++NextTargetMsgSeqNum; }

        public System.DateTime? CreationTime
        {
            get { return _creationTime; }
            internal set { _creationTime = value; }
        }

        public void Reset()
        {
            NextSenderMsgSeqNum = 1;
            NextTargetMsgSeqNum = 1;
            Messages.Clear();
            _creationTime = DateTime.UtcNow;
        }

        public void Refresh()
        { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                Messages = null;
            }
            _disposed = true;
        }

        ~MemoryStore() => Dispose(false);
        #endregion
    }
}
