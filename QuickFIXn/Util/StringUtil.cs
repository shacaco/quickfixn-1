using System.Runtime.InteropServices;

namespace QuickFix.Util
{
    public static class StringUtil
    {
        public static string FixSlashes(string s)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return s.Replace('/', '\\');
            return s.Replace('\\', '/');
        }
    }
}
