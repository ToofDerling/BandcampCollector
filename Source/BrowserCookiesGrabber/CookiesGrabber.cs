using BrowserCookiesGrabber.Chromium;
using BrowserCookiesGrabber.Firefox;
using System.Collections.Concurrent;

namespace BrowserCookiesGrabber
{
    public class CookiesGrabber
    {
        public async Task<List<BrowserCookies>> GrabCookiesAsync(string? domain = null)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var browsers = new Browser[]
            {
                new FirefoxBrowser("Firefox"),
                new ChromiumBrowser("Chrome", Path.Combine(localAppData, @"Google\Chrome\User Data"), @"Default\Network\Cookies"),
                new ChromiumBrowser("Edge", Path.Combine(localAppData, @"Microsoft\Edge\User Data"), @"Default\Network\Cookies"),
                new ChromiumBrowser("Opera", Path.Combine(appData, @"Opera Software\Opera Stable"), @"Network\Cookies")
            };

            var browserCookieLists = new ConcurrentBag<List<BrowserCookies>>();

            await Parallel.ForEachAsync(browsers,
                    async (browser, _) =>
                        browserCookieLists.Add(await browser.RetrieveCookiesAsync(domain)));

            //foreach (var browser in browsers)
            //{
            //    browserCookieLists.Add(await browser.RetrieveCookiesAsync(domain));
            //}

            var cookies = new List<BrowserCookies>();
            foreach (var browserCookieList in browserCookieLists)
            {
                cookies.AddRange(browserCookieList);
            }

            return cookies;
        }
    }
}
