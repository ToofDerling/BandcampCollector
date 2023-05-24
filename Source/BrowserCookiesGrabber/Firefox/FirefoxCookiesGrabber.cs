using Dapper;
using System.Data.SQLite;
using System.Net;

namespace BrowserCookiesGrabber.Firefox
{
    public class FirefoxCookiesGrabber
    {
        public async Task<List<BrowserCookies>> RetrieveFirefoxCookiesAsync(string? domain)
        {
            var firefoxCookies = new List<BrowserCookies>();

            // Find Firefox configuration folder
            var firefoxConfDirPath = string.Empty;

            var platform = Environment.OSVersion.Platform;

            var profileCount = 0;

            switch (platform)
            {
                case PlatformID.Unix:
                    var homeDir = Environment.GetEnvironmentVariable("HOME");
                    firefoxConfDirPath = Path.Combine(homeDir, ".mozilla", "firefox");
                    break;
                case PlatformID.Win32NT:
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    firefoxConfDirPath = Path.Combine(appData, "mozilla", "firefox");
                    break;
                default:
                    Console.WriteLine($"Unknown Firefox platform: {platform}");
                    return firefoxCookies;
            }

            // Find all firefox profiles
            var profilesListPath = Path.Combine(firefoxConfDirPath, "profiles.ini");
            var profilesListFile = new FileInfo(profilesListPath);
            if (!profilesListFile.Exists)
            {
                return firefoxCookies;
            }

            var entriesWithPath = (await File.ReadAllLinesAsync(profilesListPath)).Where(line => line.StartsWith("Path=Profiles"));

            // For each profile, look for cookies
            foreach (var entryWithPath in entriesWithPath)
            {
                var result = new Dictionary<string, string>();

                var profilePath = entryWithPath.Split('=')[1];

                var cookiesFilePath = Path.Combine(firefoxConfDirPath, profilePath, "cookies.sqlite");

                if (File.Exists(cookiesFilePath))
                {
                    // Copy cookies file as tmp file
                    var copiedCookiesPath = Path.GetTempFileName();
                    File.Copy(cookiesFilePath, copiedCookiesPath, overwrite: true);

                    IEnumerable<dynamic> rows;
                    try
                    {
                        // Start reading firefox's cookies.sqlite
                        using var sqlConnection = new SQLiteConnection($"Data Source={cookiesFilePath}");

                        if (!string.IsNullOrEmpty(domain))
                        {
                            rows = await sqlConnection.QueryAsync($"select * from moz_cookies where host = @host", new { host = domain });
                        }
                        else
                        {
                            rows = await sqlConnection.QueryAsync($"select * from moz_cookies");
                        }
                    }
                    finally
                    {
                        File.Delete(copiedCookiesPath);
                    }

                    var dictRows = rows.Cast<IDictionary<string, object>>().AsList();

                    if (dictRows.Count > 0)
                    {
                        var browser = new FirefoxBrowser("Firefox") { CookiesFile = cookiesFilePath, Profile = profileCount };
                        profileCount++;

                        var cookieContainer = new CookieContainer();

                        foreach (var row in dictRows)
                        {
                            var cookie = new Cookie()
                            {
                                Value = row["value"].ToString(),
                                Name = row["name"].ToString(),
                                Domain = row["host"].ToString(),
                                Path = row["path"].ToString(),
                                Expires = Convert.ToInt64(row["expiry"]).UnixTimeToDateTime(),
                                HttpOnly = row["isHttpOnly"].ToString() == "1",
                                Secure = row["isSecure"].ToString() == "1",
                            };

                            cookieContainer.Add(cookie);
                        }

                        firefoxCookies.Add(new BrowserCookies { Browser = browser, Cookies = cookieContainer });
                    }
                }
            }

            // Differentiate between profiles if reading cookies from more than one profile
            if (profileCount > 1)
            {
                firefoxCookies.ForEach(ff => ff.Browser.Name += $"-{((FirefoxBrowser)ff.Browser).Profile}");
            }

            return firefoxCookies;
        }
    }
}
