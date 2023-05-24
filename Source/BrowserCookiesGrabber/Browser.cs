namespace BrowserCookiesGrabber
{
    public abstract class Browser
    {
        public Browser(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public string CookiesFile { get; set; }

        public abstract Task<List<BrowserCookies>> RetrieveCookiesAsync(string? domain);
    }
}
