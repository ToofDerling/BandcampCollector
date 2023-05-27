namespace BandcampCollector
{
    public class Settings
    {
        // All properties with a public setter are read from settings file

        public static string BandcampUser { get; set; }

        public static bool SkipHiddenItems => true;

        public static bool FixBandTitleCasing => true;

        public static string AudioFormat => AudioFormats.Flac;

        public static string DownloadFolder => @"M:\BC\";

        public static string CollectionFolder => Path.Combine(DownloadFolder, "Bandcamp Collection");

        public static int ParallelDownloads => 3;

        public static int Retries => 3;

        public static ConsoleColor WorkingColor => ConsoleColor.DarkYellow;
        public static ConsoleColor OkColor => ConsoleColor.DarkGreen;
        public static ConsoleColor ErrorColor => ConsoleColor.DarkRed;
    }
}
