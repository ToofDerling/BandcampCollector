using System.Net;

namespace BrowserCookiesGrabber
{
    public class BrowserCookies
    {
        public Browser Browser { get; set; }

        public CookieContainer Cookies { get; set; }
    }
}