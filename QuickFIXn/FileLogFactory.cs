
namespace QuickFix
{
    /// <summary>
    /// Creates a message store that stores messages in a file
    /// </summary>
    public class FileLogFactory : ILogFactory
    {
        SessionSettings settings_;

        #region LogFactory Members

        public FileLogFactory(SessionSettings settings)
        {
            settings_ = settings;
        }

        /// <summary>
        /// Creates a file-based message store
        /// </summary>
        /// <param name="sessionID">session ID for the message store</param>
        /// <returns></returns>
        public ILog Create(SessionID sessionID)
        {
            var isAsync = settings_.Get(sessionID).Has(SessionSettings.ASYNC_FILE_LOG) && settings_.Get(sessionID).GetBool(SessionSettings.ASYNC_FILE_LOG);
            if (isAsync)
                return new FileLogAsync(settings_.Get(sessionID).GetString(SessionSettings.FILE_LOG_PATH), sessionID);
            return new FileLog(settings_.Get(sessionID).GetString(SessionSettings.FILE_LOG_PATH), sessionID);
        }

        #endregion
    }
}
