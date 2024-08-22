namespace QuickFix.Logger
{
    /// <summary>
    /// Creates a message store that stores messages in a file
    /// Creates a file-based message log
    /// </summary>
    public class FileLogFactory(SessionSettings settings) : ILogFactory
    {
        /// <summary>
        /// Creates a file-based message store
        /// </summary>
        /// <param name="sessionID">session ID for the message store</param>
        /// <returns></returns>
        public ILog Create(SessionID sessionID)
        {
            var isAsync = settings.Get(sessionID).Has(SessionSettings.ASYNC_FILE_LOG) && settings.Get(sessionID).GetBool(SessionSettings.ASYNC_FILE_LOG);
            if (isAsync)
                return new FileLogAsync(settings.Get(sessionID).GetString(SessionSettings.FILE_LOG_PATH), sessionID);
            return new FileLog(settings.Get(sessionID).GetString(SessionSettings.FILE_LOG_PATH), sessionID);
        }

        public ILog CreateNonSessionLog()
        {
            return new FileLog(
                settings.Get().GetString(SessionSettings.FILE_LOG_PATH),
                new SessionID("Non", "Session", "Log"));
        }
    }
}


