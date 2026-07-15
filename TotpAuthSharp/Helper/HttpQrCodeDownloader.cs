using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using TotpAuthSharp.Interface;

namespace TotpAuthSharp.Helper;

/// <summary>
///     <see cref="IQrCodeDownloader" /> backed by <see cref="HttpClient" />.
/// </summary>
public class HttpQrCodeDownloader : IQrCodeDownloader
{
    private readonly HttpClient _httpClient;

    /// <summary>
    ///     Creates a downloader. Pass an <see cref="HttpClient" /> to reuse a shared/injected instance;
    ///     when omitted a new client is created.
    /// </summary>
    public HttpQrCodeDownloader(HttpClient httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public byte[] Download(string url, int timeoutInSeconds = 30)
    {
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutInSeconds));
            var response = _httpClient.GetAsync(url, cancellation.Token).GetAwaiter().GetResult();

            if (response.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException("Unexpected result from the quickchart.io QR web site.");

            return response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            throw new HttpRequestException("Unexpected result from the quickchart.io QR web site.", exception);
        }
    }
}
