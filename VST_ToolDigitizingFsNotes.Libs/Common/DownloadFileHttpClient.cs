using System.IO;
using System.Net.Http;

namespace VST_ToolDigitizingFsNotes.Libs.Common
{
    public class DownloadFileHttpClient : HttpClient
    {
        private readonly string _userAgent;

        public DownloadFileHttpClient(string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3")
        {
            _userAgent = userAgent;
        }

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("User-Agent", _userAgent);
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            return base.SendAsync(request, cancellationToken);
        }

        public async Task<Stream> DownloadFileStreamAsync(string url)
        {
            var response = await GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStream();
        }

    }
}
