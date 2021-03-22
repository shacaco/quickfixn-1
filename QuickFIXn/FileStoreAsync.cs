using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using My_Collections;
using QuickFix.Util;

namespace QuickFix
{
    /// <summary>
    /// File store implementation
    /// </summary>
    public class FileStoreAsync : IMessageStore
    {
        private class MsgDef
        {
            internal static FactoryRepo<MsgDef> Factory = new FactoryRepo<MsgDef>(65536, () => new MsgDef(), 65000);
            public long index { get; internal set; }
            public int size { get; internal set; }

            public MsgDef(long index, int size)
            {
                this.index = index;
                this.size = size;
            }
            private MsgDef()
            { }
        }

        private readonly object _lock = new object();
        private string seqNumsFileName_;
        private string msgFileName_;
        private string headerFileName_;
        private string sessionFileName_;

        private System.IO.FileStream msgFile_;
        private System.IO.StreamWriter headerFile_;
        private System.IO.StreamWriter seqNumsWriter_;

        private MemoryStore cache_ = new MemoryStore();
        System.Collections.Generic.Dictionary<int, MsgDef> offsets_ = new Dictionary<int, MsgDef>();

        private bool _abortTask;

        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        private string _lastSequence;
        private ConcurrentQueue<ValueTuple<string, int>> _setsToWrite = new ConcurrentQueue<ValueTuple<string, int>>();
        private readonly ProducerConsumerBuffer<StringBuilder> SeqMsgBuffer = new ProducerConsumerBuffer<StringBuilder>(4, true, true, () => new StringBuilder(0.ToString("D10") + " : " + 0.ToString("D10") + " "));
        private readonly StringBuilder _setBuffer = new StringBuilder();
        private readonly Thread _setThread;

        public DateTime? CreationTime => cache_.CreationTime;

        public static string Prefix(SessionID sessionID)
        {
            System.Text.StringBuilder prefix = new System.Text.StringBuilder(sessionID.BeginString)
                .Append('-').Append(sessionID.SenderCompID);
            if (SessionID.IsSet(sessionID.SenderSubID))
                prefix.Append('_').Append(sessionID.SenderSubID);
            if (SessionID.IsSet(sessionID.SenderLocationID))
                prefix.Append('_').Append(sessionID.SenderLocationID);
            prefix.Append('-').Append(sessionID.TargetCompID);
            if (SessionID.IsSet(sessionID.TargetSubID))
                prefix.Append('_').Append(sessionID.TargetSubID);
            if (SessionID.IsSet(sessionID.TargetLocationID))
                prefix.Append('_').Append(sessionID.TargetLocationID);

            if (SessionID.IsSet(sessionID.SessionQualifier))
                prefix.Append('-').Append(sessionID.SessionQualifier);

            return prefix.ToString();
        }

        public FileStoreAsync(string path, SessionID sessionID)
        {
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            string prefix = Prefix(sessionID);

            seqNumsFileName_ = System.IO.Path.Combine(path, prefix + ".seqnums");
            msgFileName_ = System.IO.Path.Combine(path, prefix + ".body");
            headerFileName_ = System.IO.Path.Combine(path, prefix + ".header");
            sessionFileName_ = System.IO.Path.Combine(path, prefix + ".session");
            open();
            _setThread = new Thread(SetSeqNumTask){ Priority= ThreadPriority.Lowest, IsBackground = true };
            _setThread.Start();
        }

        private void open()
        {
            ConstructFromFileCache();
            InitializeSessionCreateTime();

            seqNumsWriter_ = new StreamWriter(new System.IO.FileStream(seqNumsFileName_, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite));
            msgFile_ = new System.IO.FileStream(msgFileName_, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite);
            headerFile_ = new System.IO.StreamWriter(headerFileName_, true);
        }

        private void PurgeSingleFile(System.IO.Stream stream, string filename)
        {
            if (stream != null)
                stream.Close();
            if (System.IO.File.Exists(filename))
                System.IO.File.Delete(filename);
        }

        private void PurgeSingleFile(System.IO.StreamWriter stream, string filename)
        {
            stream?.Close();
            if (System.IO.File.Exists(filename))
                System.IO.File.Delete(filename);
        }

        private void PurgeSingleFile(string filename)
        {
            if (System.IO.File.Exists(filename))
                System.IO.File.Delete(filename);
        }

        private void PurgeFileCache()
        {
            PurgeSingleFile(seqNumsWriter_, seqNumsFileName_);
            PurgeSingleFile(msgFile_, msgFileName_);
            PurgeSingleFile(headerFile_, headerFileName_);
            PurgeSingleFile(sessionFileName_);
        }

        private void ConstructFromFileCache()
        {
            offsets_.Clear();
            if (System.IO.File.Exists(headerFileName_))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(headerFileName_))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] headerParts = line.Split(',');
                        if (headerParts.Length == 3)
                        {
                            offsets_[Convert.ToInt32(headerParts[0])] = new MsgDef(
                                Convert.ToInt64(headerParts[1]), Convert.ToInt32(headerParts[2]));
                        }
                    }
                }
            }

            if (System.IO.File.Exists(seqNumsFileName_))
            {
                using (System.IO.StreamReader seqNumReader = new System.IO.StreamReader(seqNumsFileName_))
                {
                    string[] parts = seqNumReader.ReadToEnd().Split(':');
                    if (parts.Length == 2)
                    {
                        cache_.SetNextSenderMsgSeqNum(Convert.ToInt32(parts[0]));
                        cache_.SetNextTargetMsgSeqNum(Convert.ToInt32(parts[1]));
                    }
                }
            }
        }

        private void InitializeSessionCreateTime()
        {
            if (System.IO.File.Exists(sessionFileName_) && new System.IO.FileInfo(sessionFileName_).Length > 0)
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(sessionFileName_))
                {
                    string s = reader.ReadToEnd();
                    cache_.CreationTime = UtcDateTimeSerializer.FromString(s);
                }
            }
            else
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(sessionFileName_, false))
                {
                    writer.Write(UtcDateTimeSerializer.ToString(cache_.CreationTime.Value));
                }
            }
        }

        #region MessageStore Members

        /// <summary>
        /// Get messages within the range of sequence numbers
        /// </summary>
        /// <param name="startSeqNum"></param>
        /// <param name="endSeqNum"></param>
        /// <param name="messages"></param>
        public void Get(int startSeqNum, int endSeqNum, List<string> messages)
        {
            lock (_lock)
                for (int i = startSeqNum; i <= endSeqNum; i++)
                {
                    if (offsets_.ContainsKey(i))
                    {
                        msgFile_.Seek(offsets_[i].index, System.IO.SeekOrigin.Begin);
                        byte[] msgBytes = new byte[offsets_[i].size];
                        msgFile_.Read(msgBytes, 0, msgBytes.Length);

                        messages.Add(Encoding.UTF8.GetString(msgBytes));
                    }
                }
        }

        /// <summary>
        /// Store a message
        /// </summary>
        /// <param name="msgSeqNum"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Set(int msgSeqNum, string msg)
        {
            _setsToWrite.Enqueue((msg, msgSeqNum));
            _autoResetEvent.Set();
            return true;
        }

        public int GetNextSenderMsgSeqNum()
        {
            return cache_.GetNextSenderMsgSeqNum();
        }

        public int GetNextTargetMsgSeqNum()
        {
            return cache_.GetNextTargetMsgSeqNum();
        }

        public void SetNextSenderMsgSeqNum(int value)
        {
            cache_.SetNextSenderMsgSeqNum(value);
            setSeqNum();
        }

        public void SetNextTargetMsgSeqNum(int value)
        {
            cache_.SetNextTargetMsgSeqNum(value);
            setSeqNum();
        }

        public void IncrNextSenderMsgSeqNum()
        {
            cache_.IncrNextSenderMsgSeqNum();
            setSeqNum();
        }

        public void IncrNextTargetMsgSeqNum()
        {
            cache_.IncrNextTargetMsgSeqNum();
            setSeqNum();
        }

        private void setSeqNum()
        {
            _autoResetEvent.Set();
        }

        private void SetSeqNumTask()
        {
            while (true)
            {
                _autoResetEvent.WaitOne();

                if (_abortTask)
                    break;

                lock (_lock)
                {
                    while (_setsToWrite.TryDequeue(out var tuple))
                    {
                        var msg = tuple.Item1;
                        var msgSeqNum = tuple.Item2;
                        msgFile_.Seek(0, System.IO.SeekOrigin.End);

                        long offset = msgFile_.Position;
                        byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
                        int size = msgBytes.Length;

                        _setBuffer.Clear();
                        _setBuffer.Append(msgSeqNum).Append(",").Append(offset).Append(",").Append(size);
                        headerFile_.WriteLine(_setBuffer.ToString());
                        headerFile_.Flush();

                        var offsetObject = MsgDef.Factory.GetNext();
                        offsetObject.index = offset;
                        offsetObject.size = size;
                        offsets_[msgSeqNum] = offsetObject;

                        msgFile_.Write(msgBytes, 0, size);
                        msgFile_.Flush();
                    }

                    var buffer = SeqMsgBuffer.Dequeue();
                    buffer.Remove(0, 10);
                    buffer.Insert(0, GetNextSenderMsgSeqNum().ToString("D10"));
                    buffer.Remove(13, 10);
                    buffer.Insert(13, GetNextTargetMsgSeqNum().ToString("D10"));

                    seqNumsWriter_.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
                    seqNumsWriter_.Write(buffer.ToString());
                    seqNumsWriter_.Flush();
                    SeqMsgBuffer.Enqueue(buffer);
                }
            }
        }

        [System.Obsolete("Use CreationTime instead")]
        public DateTime GetCreationTime()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            lock (_lock)
            {
                cache_.Reset();
                PurgeFileCache();
                open();
            }
        }

        public void Refresh()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            lock (_lock)
            {
                _abortTask = true;
                _autoResetEvent.Set();
                _setThread.Join(500);
                seqNumsWriter_.Dispose();
                msgFile_.Dispose();
                headerFile_.Dispose();
            }
        }

        #endregion
    }
}
