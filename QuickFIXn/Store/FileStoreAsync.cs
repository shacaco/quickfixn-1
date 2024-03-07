using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using QuickFix.Util;
using Utils;

namespace QuickFix.Store
{
    /// <summary>
    /// File store implementation
    /// </summary>
    public class FileStoreAsync : IMessageStore
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private struct MsgDef
        {
            public long Index { get; }
            public int Size { get; }

            public MsgDef(long index, int size)
            {
                Index = index;
                Size = size;
            }
        }

        private readonly object _lock = new();
        private readonly string _seqNumsFileName;
        private readonly string _msgFileName;
        private readonly string _headerFileName;
        private readonly string _sessionFileName;

        private FileStream _msgFile;
        private StreamWriter _headerFile;
        private StreamWriter _seqNumsWriter;

        private readonly MemoryStore _cache = new();
        private readonly Dictionary<SeqNumType, MsgDef> _offsets = new();

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private readonly AutoResetEvent _autoResetEvent = new(false);
        private readonly ConcurrentQueue<ValueTuple<string, SeqNumType>> _setsToWrite = new();

        private readonly Thread _setThread;

        public DateTime? CreationTime => _cache.CreationTime;

        public static string Prefix(SessionID sessionID)
        {
            StringBuilder prefix = new StringBuilder(sessionID.BeginString)
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
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string prefix = Prefix(sessionID);

            _seqNumsFileName = Path.Combine(path, prefix + ".seqnums");
            _msgFileName = Path.Combine(path, prefix + ".body");
            _headerFileName = Path.Combine(path, prefix + ".header");
            _sessionFileName = Path.Combine(path, prefix + ".session");
            open();
            _setThread = new Thread(SetSeqNumTask) { IsBackground = true };
            _setThread.Start();
        }

        private void open()
        {
            ConstructFromFileCache();
            InitializeSessionCreateTime();

            _seqNumsWriter = new StreamWriter(new FileStream(_seqNumsFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite));
            _msgFile = new FileStream(_msgFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            _headerFile = new StreamWriter(_headerFileName, true);
        }

        private void PurgeSingleFile(Stream stream, string filename)
        {
            if (stream != null)
                stream.Close();
            if (File.Exists(filename))
                File.Delete(filename);
        }

        private void PurgeSingleFile(StreamWriter stream, string filename)
        {
            stream?.Close();
            if (File.Exists(filename))
                File.Delete(filename);
        }

        private void PurgeSingleFile(string filename)
        {
            if (File.Exists(filename))
                File.Delete(filename);
        }

        private void PurgeFileCache()
        {
            PurgeSingleFile(_seqNumsWriter, _seqNumsFileName);
            PurgeSingleFile(_msgFile, _msgFileName);
            PurgeSingleFile(_headerFile, _headerFileName);
            PurgeSingleFile(_sessionFileName);
        }

        private void ConstructFromFileCache()
        {
            _offsets.Clear();
            if (File.Exists(_headerFileName))
            {
                using (StreamReader reader = new StreamReader(_headerFileName))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] headerParts = line.Split(',');
                        if (headerParts.Length == 3)
                        {
                            _offsets[Convert.ToUInt64(headerParts[0])] = new MsgDef(
                                Convert.ToInt64(headerParts[1]), Convert.ToInt32(headerParts[2]));
                        }
                    }
                }
            }

            if (File.Exists(_seqNumsFileName))
            {
                using (StreamReader seqNumReader = new StreamReader(_seqNumsFileName))
                {
                    string[] parts = seqNumReader.ReadToEnd().Split(':');
                    if (parts.Length == 2)
                    {
                        _cache.NextSenderMsgSeqNum = Convert.ToUInt64(parts[0]);
                        _cache.NextTargetMsgSeqNum = Convert.ToUInt64(parts[1]);
                    }
                }
            }
        }

        private void InitializeSessionCreateTime()
        {
            if (File.Exists(_sessionFileName) && new FileInfo(_sessionFileName).Length > 0)
            {
                using (StreamReader reader = new StreamReader(_sessionFileName))
                {
                    string s = reader.ReadToEnd();
                    _cache.CreationTime = UtcDateTimeSerializer.FromString(s);
                }
            }
            else
            {
                using (StreamWriter writer = new StreamWriter(_sessionFileName, false))
                {
                    writer.Write(UtcDateTimeSerializer.ToString(_cache.CreationTime.Value));
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
        public void Get(SeqNumType startSeqNum, SeqNumType endSeqNum, List<string> messages)
        {
            lock (_lock)
                for (ulong i = startSeqNum; i <= endSeqNum; i++)
                {
                    if (_offsets.ContainsKey(i))
                    {
                        _msgFile.Seek(_offsets[i].Index, SeekOrigin.Begin);
                        byte[] msgBytes = new byte[_offsets[i].Size];
                        _msgFile.Read(msgBytes, 0, msgBytes.Length);
                        var data = CharEncoding.DefaultEncoding.GetString(msgBytes);
                        messages.Add(data);
                    }
                }
        }

        /// <summary>
        /// Store a message
        /// </summary>
        /// <param name="msgSeqNum"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool Set(SeqNumType msgSeqNum, ReadOnlySpan<char> msg)
        {
            _setsToWrite.Enqueue((msg.ToString(), msgSeqNum));
            _autoResetEvent.Set();
            return true;
        }

        public ulong NextSenderMsgSeqNum
        {
            get { return _cache.NextSenderMsgSeqNum; }
            set
            {
                _cache.NextSenderMsgSeqNum = value;
                setSeqNum();
            }
        }

        public ulong NextTargetMsgSeqNum
        {
            get { return _cache.NextTargetMsgSeqNum; }
            set
            {
                _cache.NextTargetMsgSeqNum = value;
                setSeqNum();
            }
        }

        public void IncrNextSenderMsgSeqNum()
        {
            _cache.IncrNextSenderMsgSeqNum();
            setSeqNum();
        }

        public void IncrNextTargetMsgSeqNum()
        {
            _cache.IncrNextTargetMsgSeqNum();
            setSeqNum();
        }

        private void setSeqNum()
        {
            _autoResetEvent.Set();
        }

        private void SetSeqNumTask()
        {
            if (ApplicationPrivileges.Configuration.ThreadPriorityEnabled)
            {
                Logger.Info($"Setting thread priority to {ThreadPriority.Lowest}");
                ApplicationPrivileges.ThreadPrivileges.SetCurrentThreadPriority(ThreadPriority.Lowest);
            }

            var writeBuffer = new byte[1024];
            var setBuffer = new StringBuilder();
            var seqMsgBuffer = new StringBuilder(0.ToString("D20") + " : " + 0.ToString("D20") + " ");

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                _autoResetEvent.WaitOne();

                if(_cancellationTokenSource.IsCancellationRequested)
                    break;

                lock (_lock)
                {
                    while (_setsToWrite.TryDequeue(out var tuple))
                    {
                        var msg = tuple.Item1;
                        var msgSeqNum = tuple.Item2;
                        _msgFile.Seek(0, SeekOrigin.End);

                        long offset = _msgFile.Position;
                        var length = CharEncoding.DefaultEncoding.GetBytes(msg, writeBuffer);

                        setBuffer.Clear();
                        setBuffer.Append(msgSeqNum).Append(",").Append(offset).Append(",").Append(length);
                        _headerFile.WriteLine(setBuffer.ToString());
                        _headerFile.Flush();

                        var offsetObject = new MsgDef(offset, length);
                        _offsets[msgSeqNum] = offsetObject;

                        _msgFile.Write(writeBuffer, 0, length);
                        _msgFile.Flush();
                    }

                    seqMsgBuffer.Remove(0, 20);
                    seqMsgBuffer.Insert(0, NextSenderMsgSeqNum.ToString("D20"));
                    seqMsgBuffer.Remove(23, 20);
                    seqMsgBuffer.Insert(23, NextTargetMsgSeqNum.ToString("D20"));

                    _seqNumsWriter.BaseStream.Seek(0, SeekOrigin.Begin);
                    _seqNumsWriter.Write(seqMsgBuffer.ToString());
                    _seqNumsWriter.Flush();
                }
            }
        }

        [Obsolete("Use CreationTime instead")]
        public DateTime GetCreationTime()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            lock (_lock)
            {
                _cache.Reset();
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
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _autoResetEvent.Set();
                _setThread.Join(500);
                _seqNumsWriter.Dispose();
                _msgFile.Dispose();
                _headerFile.Dispose();
            }
        }

        #endregion
    }
}
