using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TotpAuthSharp.Helper;
using Xunit;

namespace TotpAuthSharp.Tests;

public class HttpQrCodeDownloaderTests
{
    private static readonly byte[] Payload = "downloaded-qr"u8.ToArray();

    [Fact]
    public void Download_OnSuccess_ReturnsResponseBytes()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, Payload);
        var downloader = new HttpQrCodeDownloader(new HttpClient(handler));

        var bytes = downloader.Download("https://quickchart.io/chart?cht=qr&chl=test");

        Assert.Equal(Payload, bytes);
        Assert.Equal("https://quickchart.io/chart?cht=qr&chl=test", handler.LastRequestUri);
    }

    [Fact]
    public void Download_OnNonSuccessStatus_ThrowsHttpRequestException()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.InternalServerError, Array.Empty<byte>());
        var downloader = new HttpQrCodeDownloader(new HttpClient(handler));

        Assert.Throws<HttpRequestException>(() => downloader.Download("https://quickchart.io/chart"));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly byte[] _content;

        public StubHttpMessageHandler(HttpStatusCode statusCode, byte[] content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        public string LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString();
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new ByteArrayContent(_content)
            });
        }
    }
}
