namespace BrowserCookiesGrabber.Chromium
{
    public class ChromiumBrowser : Browser
    {
        private readonly string _basePath;
        private readonly string _cookiesPath;

        public ChromiumBrowser(string name, string basePath, string cookiesPath) : base(name)
        {
            _basePath = basePath;
            _cookiesPath = cookiesPath;

            CookiesFile = Path.Combine(_basePath, _cookiesPath);
        }

        public override async Task<List<BrowserCookies>> RetrieveCookiesAsync(string? domain)
        {
            var result = new List<BrowserCookies>();

            if (Environment.OSVersion.Platform != PlatformID.Win32NT || !Directory.Exists(_basePath) || !File.Exists(CookiesFile))
            {
                return await Task.FromResult(result);
            }

            var grabber = new ChromiumCookiesGrabber(_basePath, CookiesFile);
            var cookies = await grabber.GetCookiesAsync(domain);

            result.Add(new BrowserCookies { Browser = this, Cookies = cookies });
            return result;
        }
    }
}
