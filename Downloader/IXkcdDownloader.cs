using Orleans;
using Orleans.Concurrency;

namespace comic_downloader_orleans.Downloader;

public interface IXkcdDownloader : IComicDownloader, IGrainWithIntegerKey
{
    
}

public class XkcdDownloader : Grain, IXkcdDownloader
{
    private readonly IHttpClientFactory _factory;

    public XkcdDownloader(IHttpClientFactory factory)
    {
        _factory = factory;
    }
    
    public async Task<Immutable<byte[]>> Download()
    {
        var httpClient = _factory.CreateClient();

        var result = await httpClient.GetFromJsonAsync<XkcdImageData>("https://xkcd.com/info.0.json");

        var bytes = await httpClient.GetByteArrayAsync(result.img);
        return bytes.AsImmutable();
    }
}

public class XkcdImageData
{
    public string img { get; set; }
}