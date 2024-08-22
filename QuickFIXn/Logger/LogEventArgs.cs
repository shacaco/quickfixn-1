using NLog;

namespace QuickFix.Logger
{
    public class LogEventArgs
    {
        public SessionID SessionID { get; }
        public string Message { get; }
        public LogLevel LogLevel { get; }

        public LogEventArgs(string message, LogLevel logLevel, SessionID sessionID)
        {
            Message = message;
            LogLevel = logLevel;
            SessionID = sessionID;
        }
    }
}