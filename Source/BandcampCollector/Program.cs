﻿using BandcampCollector.Shared.Helpers;
using System.Runtime.InteropServices;

namespace BandcampCollector
{
    internal class Program
    {
        public static string _usage = @"
AzwConvert [or Azw Convert] <file or directory>
    Scans AzwDir and converts all unconverted comic books to cbz files.
    Specify <file or directory> to convert azw/azw3 files directly.  

AzwScan [or Azw Scan] <file or directory>
    Scans AzwDir and creates a .NEW title file for each unconverted comic book. 
    Specify <file or directory> to scan azw/azw3 files directly.  

PdfConvert [or Pdf Convert] <pdf file> or <directory with pdf files>
    Converts one or more pdf comic books to cbz files.

Commands are case insensitive. 
";
        /*
        BlackSteedConvert [BlackSteed Convert] <directory>
            Convert one or more Black Steed comic books copied from a mobile device.
        */

        static async Task Main(string[] args)
        {
            var validAction = false;
            BandcampCollectorAction action;

            var actionStr = string.Empty;
            var next = 0;

            if (args.Length > next)
            {
                ParseActionString();

                if (args.Length > next && !validAction)
                {
                    ParseActionString();
                }

                try
                {
                    if (validAction)
                    {
                        var mapper = new CollectionConnecter();

                        switch (action)
                        {
                            case BandcampCollectorAction.Map:
                                {
                                    await mapper.MapCollectionAsync();
                                }
                                break;
                            case BandcampCollectorAction.Download:
                                {
                                    await mapper.MapCollectionAsync();
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ProgressReporter.Error("CbzMage fatal error.", ex);
                }
            }

            if (!validAction)
            {
                ProgressReporter.Info(_usage);
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // If this is run as a "gui" let the console hang around
                if (ConsoleWillBeDestroyedAtTheEnd())
                {
                    Console.ReadLine();
                }
            }

            void ParseActionString()
            {
                actionStr += args[next];

                validAction = Enum.TryParse(actionStr, ignoreCase: true, out action);

                next++;
            }
        }

        private static bool ConsoleWillBeDestroyedAtTheEnd()
        {
            var processList = new uint[1];
            var processCount = GetConsoleProcessList(processList, 1);

            return processCount == 1;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetConsoleProcessList(uint[] processList, uint processCount);
    }
}