using Orleans;
using Orleans.Concurrency;

namespace comic_downloader_orleans.Downloader;

public interface ITuDownloader : IComicDownloader, IGrainWithStringKey
{
    
}

public class TuDownloader : Grain, ITuDownloader
{
    private readonly IHttpClientFactory _factory;

    public TuDownloader(IHttpClientFactory factory)
    {
        _factory = factory;
    }
    
    
    public async Task<Immutable<byte[]>> Download()
    {
        var httpClient = _factory.CreateClient();

        var bytes = await httpClient.GetByteArrayAsync($"https://www.tu.no/api/widgets/comics?name={this.GetPrimaryKeyString()}&date={DateTime.Now:yyyy-MM-dd}");

        return bytes.AsImmutable();
    }
}