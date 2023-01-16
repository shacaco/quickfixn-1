
using System;

namespace QuickFix
{
    /// <summary>
    /// Log implementation that does not do anything
    /// </summary>
    public sealed class NullLog : ILog
    {
        #region ILog Members

        public void Clear()
        { }

        public void OnIncoming(ReadOnlySpan<char> msg)
        { }

        public void OnOutgoing(ReadOnlySpan<char> msg)
        { }

        public void OnEvent(string s)
        { }

        public void Dispose()
        { }

        #endregion
    }
}
