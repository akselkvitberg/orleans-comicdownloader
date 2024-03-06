using Orleans;
using Orleans.Concurrency;

namespace comic_downloader_orleans.Downloader;

public interface IUrlDownloader : IComicDownloader, IGrainWithStringKey
{
}

public class UrlDownloader : Grain, IUrlDownloader
{
    private readonly IHttpClientFactory _factory;

    public UrlDownloader(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<Immutable<byte[]>> Download()
    {
        var httpClient = _factory.CreateClient();

        var urlTemplate = this.GetPrimaryKeyString();
        var imageUrl = string.Format(urlTemplate, DateTime.Now.Year);

        var bytes = await httpClient.GetByteArrayAsync(imageUrl);

        return bytes.AsImmutable();
    }
}