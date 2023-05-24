using System.Net;
using System.Text;

namespace BandcampCollector
{
    public static class Extensions
    {
        // IDisposable

        public static void DisposeDisposable(this IDisposable disposable)
        {
            try
            {
                disposable?.Dispose();    
            }
            catch 
            { 
            }
        }

        // Exception

        public static string TypeAndMessage(this Exception ex) => $"{ex.GetType().Name}: {ex.Message}";

        // Filesystem strings

        private static readonly char[] _invalidChars = Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()).ToArray();

        private const int _spaceChar = 32;

        public static string ToFileSystemString(this string str)
        {
            str = WebUtility.HtmlDecode(str);

            var sb = new StringBuilder(str);

            for (int i = 0, sz = sb.Length; i < sz; i++)
            {
                var ch = sb[i];
                if (_invalidChars.Contains(ch) || ch != _spaceChar && char.IsWhiteSpace(ch))
                {
                    sb[i] = ' ';
                }
            }

            return sb.Replace("   ", " ").Replace("  ", " ").ToString().Trim();
        }
    }
}
