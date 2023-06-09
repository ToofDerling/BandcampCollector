﻿namespace BandcampCollector.Shared.Extensions
{
    public static class StringExtensions
    {
        public static bool EqualsIgnoreCase(this string a, string b) => string.Compare(a, b, ignoreCase: true) == 0;

        public static bool EndsWithIgnoreCase(this string a, string endsWith) => a.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase);

        public static void CreateDirIfNotExists(this string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
