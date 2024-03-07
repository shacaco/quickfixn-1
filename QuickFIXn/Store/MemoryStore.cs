using QuickFix.Store;
using System;
using System.Collections.Generic;

namespace QuickFix.Store
{
    /// <summary>
    /// In-memory message store implementation
    /// </summary>
    public class MemoryStore : IMessageStore
    {
        #region Private Members

        internal System.Collections.Generic.Dictionary<SeqNumType, string> Messages { get; private set; }
        private DateTime? _creationTime;

        #endregion

        public MemoryStore()
        {
            Messages = new System.Collections.Generic.Dictionary<SeqNumType, string>();
            Reset();
        }

        public void Get(SeqNumType begSeqNo, SeqNumType endSeqNo, List<string> messages)
        {
            for (SeqNumType current = begSeqNo; current <= endSeqNo; current++)
            {
                if (Messages.ContainsKey(current))
                    messages.Add(Messages[current]);
            }
        }

        #region MessageStore Members

        public bool Set(SeqNumType msgSeqNum, ReadOnlySpan<char> msg)
        {
            Messages[msgSeqNum] = msg.ToString();
            return true;
        }

        public SeqNumType NextSenderMsgSeqNum { get; set; }
        public SeqNumType NextTargetMsgSeqNum { get; set; }

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
