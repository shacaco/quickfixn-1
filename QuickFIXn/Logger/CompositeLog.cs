#nullable enable
using NLog;
using System;

namespace QuickFix.Logger;

/// <summary>
/// File log implementation
/// </summary>
internal class CompositeLog : ILog
{
    private readonly ILog[] _logs;

    private bool _disposed = false;

    public CompositeLog(ILog[] logs)
    {
        _logs = logs;
    }

    public void Clear()
    {
        DisposedCheck();
        foreach (var log in _logs)
            log.Clear();
    }

    public void OnIncoming(ReadOnlySpan<char> msg)
    {
        DisposedCheck();
        foreach (var log in _logs)
            log.OnIncoming(msg);
    }

    public void OnOutgoing(ReadOnlySpan<char> msg)
    {
        DisposedCheck();
        foreach (var log in _logs)
            log.OnOutgoing(msg);
    }

    public void OnEvent(string s)
    {
        OnEvent(s, LogLevel.Info);
    }

    public void OnEvent(string s, LogLevel logLevel)
    {
        DisposedCheck();
        foreach (var log in _logs)
            log.OnEvent(s, logLevel);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            foreach (var log in _logs)
                log.Dispose();
        }
        _disposed = true;
    }

    private void DisposedCheck()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    ~CompositeLog() => Dispose(false);
}
