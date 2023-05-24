namespace BrowserCookiesGrabber.Firefox
{
    public class FirefoxBrowser : Browser
    {
        public FirefoxBrowser(string name) : base(name)
        {
        }

        public int Profile { get; set; }

        public override async Task<List<BrowserCookies>> RetrieveCookiesAsync(string? domain)
        {
            var ffCookies = new FirefoxCookiesGrabber();
            var firefoxCookies = await ffCookies.RetrieveFirefoxCookiesAsync(domain);

            return firefoxCookies;
        }
    }
}
