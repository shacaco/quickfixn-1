#nullable enable

using NLog;
using System;

namespace QuickFix.Logger;

/// <summary>
/// FIXME - needs to log sessionIDs, timestamps, etc.
/// </summary>
public class ScreenLog : ILog
{
    private readonly object _sync = new ();
    private readonly bool _logIncoming;
    private readonly bool _logOutgoing;
    private readonly bool _logEvent;

    public ScreenLog(bool logIncoming, bool logOutgoing, bool logEvent)
    {
        _logIncoming = logIncoming;
        _logOutgoing = logOutgoing;
        _logEvent    = logEvent;
    }

    #region ILog Members

    public void Clear()
    { }

    public void OnIncoming(ReadOnlySpan<char> msg)
    {
        if (!_logIncoming)
            return;

        lock (_sync)
        {
            System.Console.WriteLine("<incoming> " + msg.ToString().Replace(Message.Message.SohChar, '|'));
        }
    }

    public void OnOutgoing(ReadOnlySpan<char> msg)
    {
        if (!_logOutgoing)
            return;

        lock (_sync)
        {
            System.Console.WriteLine("<outgoing> " + msg.ToString().Replace(Message.Message.SohChar, '|'));
        }
    }

    public void OnEvent(string s)
    {
        OnEvent(s, LogLevel.Info);
    }

    public void OnEvent(string s, LogLevel logLevel)
    {
        if (!_logEvent)
            return;

        lock (_sync)
        {
            System.Console.WriteLine($"<event> {logLevel} {s}");
        }
    }
    #endregion

    #region IDisposable implementation
    public void Dispose()
    {
        Dispose(true);
        System.GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        // Nothing to dispose of...
    }
    ~ScreenLog() => Dispose(false);
    #endregion
}
