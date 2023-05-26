using System.Net;

namespace BandcampCollector
{
    public class HttpDownloader
    {
        public static HttpClient Client { get; set; }

        public static void CreateHttpClient(CookieContainer cookies)
        {
            var handler = new SocketsHttpHandler() { CookieContainer = cookies };
            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromMinutes(60);

            Client = client;
        }
    }
}
