#nullable enable
using NLog;
using System;

namespace QuickFix.Logger;

/// <summary>
/// Session log for messages and events
/// </summary>
public interface ILog : IDisposable
{
    /// <summary>
    /// event for event log messages
    /// </summary>
    public event EventHandler<LogEventArgs> LogEvent;

    /// <summary>
    /// the session ID
    /// </summary>
    public SessionID? SessionID { get; }
    /// <summary>
    /// Clears the log and removes any persistent log data
    /// </summary>
    void Clear();

    /// <summary>
    /// Logs an incoming message
    /// </summary>
    /// <param name="msg">a raw FIX message</param>
    void OnIncoming(ReadOnlySpan<char> msg);

    /// <summary>
    /// Logs an outgoing message
    /// </summary>
    /// <param name="msg">a raw FIX message</param>
    void OnOutgoing(ReadOnlySpan<char> msg);

    /// <summary>
    /// Logs a session event
    /// </summary>
    /// <param name="s">event description</param>
    void OnEvent(string s);

    /// <summary>
    /// Logs a session event
    /// </summary>
    /// <param name="s">event description</param>
    /// <param name="logLevel"></param>
    void OnEvent(string s, LogLevel logLevel);
}
