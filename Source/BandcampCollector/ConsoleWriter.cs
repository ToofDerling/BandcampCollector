namespace BandcampCollector
{
    internal class ConsoleWriter
    {
        private static int _cursorTop;
        private static bool _isCursorTopSet = false;

        public static void SetCursorTop()
        {
            if (_isCursorTopSet)
            {
                throw new InvalidOperationException($"Cursor top already set to {_cursorTop}");
            }

            _cursorTop = Console.CursorTop;
            _isCursorTopSet= true;
        }

        public static int GetCursorTop() => _cursorTop;

        private static readonly object _consoleLock = new();

        public static void WriteAt(string line, int consoleRow)
        { 
            lock (_consoleLock) 
            {
                Console.SetCursorPosition(0, consoleRow);
                Console.WriteLine(line);
            }
        }

        public static void WriteAt(string first, string last, ConsoleColor lastColor, int consoleRow)
        {
            lock (_consoleLock)
            {
                Console.SetCursorPosition(0, consoleRow);
                Console.Write(first);

                var oldColor = Console.ForegroundColor;

                Console.ForegroundColor = lastColor;
                Console.Write(last);

                Console.ForegroundColor = oldColor;
            
                Console.WriteLine();
            }
        }

        public static void WriteAt(string pre, string releaseName, string releaseInfo, ConsoleColor consoleColor, int consoleRow, string end, string errorState = null, Exception ex = null)
        {
            lock (_consoleLock)
            {
                WriteReleaseInfo(pre, releaseName, releaseInfo, consoleColor, consoleRow);

                if (errorState != null)
                {
                    WriteError(errorState, ex);
                }

                Console.WriteLine(end);
            }
        }

        private static void WriteReleaseInfo(string pre, string releaseName, string releaseInfo, ConsoleColor consoleColor, int consoleRow)
        {
            Console.SetCursorPosition(0, consoleRow);
            Console.Write(pre);

            var oldColor = Console.ForegroundColor;

            Console.ForegroundColor = consoleColor;
            Console.Write(releaseName);

            Console.ForegroundColor = oldColor;

            if (!string.IsNullOrEmpty(releaseInfo))
            {
                Console.Write(releaseInfo);
            }
        }

        private static void WriteError(string error, Exception ex)
        {
            var oldColor = Console.ForegroundColor;

            Console.ForegroundColor = Settings.ErrorColor;
            Console.Write($"{error} {ex.GetType().Name}");

            Console.ForegroundColor = oldColor;
        }
    }
}
