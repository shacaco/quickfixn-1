
using System;
using System.Collections.Concurrent;
using System.Data.SqlTypes;
using System.Threading;
using My_Collections;
using System.Linq;
using QuickFix.Fields.Converters;

namespace QuickFix
{
    /// <summary>
    /// File log implementation
    /// </summary>
    public class FileLogAsync : ILog, System.IDisposable
    {
        private const char NullChar = '\0';

        private static readonly char[] Colon = " : ".ToCharArray();
        private object sync_ = new object();

        private System.IO.StreamWriter messageLog_;
        private System.IO.StreamWriter eventLog_;

        private string messageLogFileName_;
        private string eventLogFileName_;

        private bool _abortTask;
        private bool _disposed;

        private readonly ProducerConsumerBuffer<WritePackage> _buffer = new ProducerConsumerBuffer<WritePackage>(4000, () => new WritePackage());
        private Thread _writeThread;
        private readonly ConcurrentQueue<WritePackage> _messages = new ConcurrentQueue<WritePackage>();
        private readonly ConcurrentQueue<WritePackage> _events = new ConcurrentQueue<WritePackage>();
        private readonly AutoResetEvent _writeEvent = new AutoResetEvent(true);

        public FileLogAsync(string fileLogPath)
        {
            Init(fileLogPath, "GLOBAL");
        }

        public FileLogAsync(string fileLogPath, SessionID sessionID)
        {
            Init(fileLogPath, Prefix(sessionID));
        }


        private void Init(string fileLogPath, string prefix)
        {
            if (!System.IO.Directory.Exists(fileLogPath))
                System.IO.Directory.CreateDirectory(fileLogPath);

            messageLogFileName_ = System.IO.Path.Combine(fileLogPath, prefix + ".messages.current.log");
            eventLogFileName_ = System.IO.Path.Combine(fileLogPath, prefix + ".event.current.log");

            messageLog_ = new System.IO.StreamWriter(messageLogFileName_, true);
            eventLog_ = new System.IO.StreamWriter(eventLogFileName_, true);

            messageLog_.AutoFlush = true;
            eventLog_.AutoFlush = true;

            _writeThread = new Thread(Write) { Priority = ThreadPriority.Lowest, IsBackground = true };
            _writeThread.Start();
        }

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

        private void DisposedCheck()
        {
            if (_disposed)
                throw new System.ObjectDisposedException(this.GetType().Name);
        }

        #region Log Members

        public void Clear()
        {
            DisposedCheck();

            lock (sync_)
            {
                messageLog_.Close();
                eventLog_.Close();

                messageLog_ = new System.IO.StreamWriter(messageLogFileName_, false);
                eventLog_ = new System.IO.StreamWriter(eventLogFileName_, false);

                messageLog_.AutoFlush = true;
                eventLog_.AutoFlush = true;
            }
        }

        public void OnIncoming(ReadOnlySpan<char> msg)
        {
            AddWriteOperation(_messages, msg);
        }

        public void OnOutgoing(ReadOnlySpan<char> msg)
        {
            AddWriteOperation(_messages, msg);
        }

        public void OnEvent(string msg)
        {
            AddWriteOperation(_events, msg);
        }

        private void AddWriteOperation(ConcurrentQueue<WritePackage> dest, ReadOnlySpan<char> msg)
        {
            var package = _buffer.Dequeue();
            package.Time = DateTime.UtcNow;
            msg.CopyTo(package.Buffer.AsSpan());
            package.Length = msg.Length;
            dest.Enqueue(package);
            _writeEvent.Set();
        }

        private void Write()
        {
            try
            {
                while (!_abortTask)
                {
                    _writeEvent.WaitOne();
                    lock (sync_)
                    {
                        DisposedCheck();
                        while (_messages.TryDequeue(out var package))
                        {
                            var timeStr = DateTimeConverter.Convert(package.Time, TimeStampPrecision.Microsecond).AsSpan();
                            messageLog_.Write(timeStr);
                            messageLog_.Write(Colon);
                            messageLog_.WriteLine(package.Buffer, 0, package.Length);
                            _buffer.Enqueue(package);
                        }

                        while (_events.TryDequeue(out var package))
                        {
                            var timeStr = DateTimeConverter.Convert(package.Time, TimeStampPrecision.Microsecond).AsSpan();
                            eventLog_.Write(timeStr);
                            eventLog_.Write(Colon);
                            eventLog_.WriteLine(package.Buffer, 0, package.Length);
                            _buffer.Enqueue(package);
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            { }
        }

        class WritePackage
        {
            internal char[] Buffer { get; } = new char[1024];
            internal int Length { get; set; }
            internal DateTime Time { get; set; }
        }
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _abortTask = true;
            _writeThread.Join(500);
            _writeEvent.Set();

            if (messageLog_ != null) { messageLog_.Dispose(); }
            if (eventLog_ != null) { eventLog_.Dispose(); }

            messageLog_ = null;
            eventLog_ = null;
            _disposed = true;
        }

        #endregion
    }
}
