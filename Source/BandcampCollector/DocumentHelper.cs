using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using System.Net;

namespace BandcampCollector
{
    public class DocumentHelper
    {
        public static string GetDecodedDataBlob(HtmlNode doc)
        {
            var pageData = doc.QuerySelector("#pagedata");
            var data = pageData.Attributes["data-blob"].Value;

            data = WebUtility.HtmlDecode(data);
            return data;
        }

        public static async Task<HtmlNode> GetDocumentAsync(HttpResponseMessage response)
        {
            var page = await response.Content.ReadAsStringAsync();

            var html = new HtmlDocument();
            html.LoadHtml(page);

            var doc = html.DocumentNode;
            return doc;
        }
    }
}
