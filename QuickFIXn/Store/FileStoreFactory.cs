#nullable enable
using QuickFix;
using QuickFix.Store;

/// <summary>
/// Creates a message store that stores messages in a file
/// </summary>
public class FileStoreFactory : IMessageStoreFactory
{
    private readonly SessionSettings _settings;

    /// <summary>
    /// Create the factory with configuration in session settings
    /// </summary>
    /// <param name="settings"></param>
    public FileStoreFactory(SessionSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Creates a file-based message store
    /// </summary>
    /// <param name="sessionId">session ID for the message store</param>
    /// <returns></returns>
    public IMessageStore Create(SessionID sessionId)
    {
        return new FileStore(_settings.Get(sessionId).GetString(SessionSettings.FILE_STORE_PATH), sessionId);
    }
}

namespace QuickFix
{
    /// <summary>
    /// Creates a message store that stores messages in a file
    /// </summary>
    public class FileStoreFactory : IMessageStoreFactory
    {
        private SessionSettings _settings;

        /// <summary>
        /// Create the factory with configuration in session settings
        /// </summary>
        /// <param name="settings"></param>
        public FileStoreFactory(SessionSettings settings)
        {
            _settings = settings;
        }

        #region MessageStoreFactory Members

        /// <summary>
        /// Creates a file-based message store
        /// </summary>
        /// <param name="sessionID">session ID for the message store</param>
        /// <returns></returns>
        public IMessageStore Create(SessionID sessionID)
        {
            var isAsync = _settings.Get(sessionID).Has(SessionSettings.ASYNC_FILE_STORE) && _settings.Get(sessionID).GetBool(SessionSettings.ASYNC_FILE_STORE);
            if (isAsync)
                return new FileStoreAsync(_settings.Get(sessionID).GetString(SessionSettings.FILE_STORE_PATH), sessionID);
            return new FileStore(_settings.Get(sessionID).GetString(SessionSettings.FILE_STORE_PATH), sessionID);
        }

        #endregion
    }
}

