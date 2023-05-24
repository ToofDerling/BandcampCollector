namespace BandcampCollector
{
    public class Settings
    {
        public static string BandcampUser => "metalbandcamp";

        public static bool SkipHiddenItems => true;

        public static bool FixArtistTitleCasing => true;

        public static string AudioFormat => AudioFormats.Flac;

        public static string DownloadFolder => @"M:\BC\_test";

        public static string CollectionFolder => Path.Combine(DownloadFolder, "Bandcamp Collection");

        public static int ParallelDownloads => 3;

        public static int Retries => 3;

        public static ConsoleColor WorkingColor => ConsoleColor.DarkYellow;
        public static ConsoleColor OkColor => ConsoleColor.DarkGreen;
        public static ConsoleColor ErrorColor => ConsoleColor.DarkRed;
    }
}
