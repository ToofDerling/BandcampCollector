﻿using BandcampCollector.Shared.Helpers;
using BandcampCollector.Shared.Settings;

namespace BandcampCollector
{
    public class BandcampCollectorSettings
    {
        public static Settings Settings => new();

        private readonly SharedSettings _settingsHelper = new();

        public void CreateSettings()
        {
            _settingsHelper.CreateSettings(nameof(BandcampCollectorSettings), Settings);

            ConfigureSettings();
        }

        // Defaults
        private const string _defaultTitlesDir = "Titles";
        private const string _defaultCbzDir = "Cbz Backups";

        private const string _defaultConvertedTitlesDirName = "Converted Titles";
        private const string _defaultNewTitleMarker = ".NEW";
        private const string _defaultUpdateTitleMarker = ".UPDATED";

        private void ConfigureSettings()
        {
            //AzwDir
            if (string.IsNullOrWhiteSpace(Settings.BandcampUser))
            {
                ProgressReporter.Warning($"{nameof(Settings.BandcampUser)} is not configured in BandcampCollectorSettings.json");
            }

            ////TitlesDir
            //if (string.IsNullOrWhiteSpace(Settings.TitlesDir))
            //{
            //    var dir = new DirectoryInfo(Settings.AzwDir).Parent;
            //    Settings.TitlesDir = Path.Combine(dir.FullName, _defaultTitlesDir);
            //}
            //Settings.TitlesDir.CreateDirIfNotExists();

            ////SaveCover/SaveCoverOnly
            //Settings.SaveCoverOnly = Settings.SaveCoverOnly && Settings.SaveCover;

            ////SaveCoverDir
            //if (Settings.SaveCover && !string.IsNullOrWhiteSpace(Settings.SaveCoverDir))
            //{
            //    Settings.SaveCoverDir.CreateDirIfNotExists();
            //}
            //else
            //{
            //    Settings.SaveCoverDir = null;
            //}

            ////ConvertedTitlesDirName
            //if (string.IsNullOrWhiteSpace(Settings.ConvertedTitlesDirName))
            //{
            //    Settings.ConvertedTitlesDirName = _defaultConvertedTitlesDirName;
            //}
            //Settings.SetConvertedTitlesDir(Path.Combine(Settings.TitlesDir, Settings.ConvertedTitlesDirName));
            //Settings.ConvertedTitlesDir.CreateDirIfNotExists();

            ////CbzDir
            //if (string.IsNullOrWhiteSpace(Settings.CbzDir))
            //{
            //    var dir = new DirectoryInfo(Settings.AzwDir).Parent;
            //    Settings.CbzDir = Path.Combine(dir.FullName, _defaultCbzDir);
            //    Settings.CbzDir.CreateDirIfNotExists();

            //    Settings.SetCbzDirSetBySystem();
            //}

            ////AnalysisDir
            //if (!string.IsNullOrWhiteSpace(Settings.AnalysisDir))
            //{
            //    Settings.AnalysisDir.CreateDirIfNotExists();
            //}

            ////NewTitleMarker/UpdatedTitleMarker
            //Settings.NewTitleMarker = string.IsNullOrWhiteSpace(Settings.NewTitleMarker)
            //    ? _defaultNewTitleMarker
            //    : Settings.NewTitleMarker;

            //Settings.UpdatedTitleMarker = string.IsNullOrWhiteSpace(Settings.UpdatedTitleMarker)
            //    ? _defaultUpdateTitleMarker
            //    : Settings.UpdatedTitleMarker;

            //Settings.SetAllMarkers();

            ////TrimPublishers
            //Settings.TrimPublishers ??= Array.Empty<string>();

            ////NumberOfThreads
            //Settings.NumberOfThreads = _settingsHelper.GetThreadCount(Settings.NumberOfThreads);
            //Settings.SetParallelOptions(new ParallelOptions { MaxDegreeOfParallelism = Settings.NumberOfThreads });
        }
    }
}
