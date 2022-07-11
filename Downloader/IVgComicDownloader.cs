using Orleans;
using Orleans.Concurrency;

namespace comic_downloader_orleans.Downloader;

public interface IVgComicDownloader : IComicDownloader, IGrainWithStringKey
{
    
}


public class VgComicDownloader : Grain, IVgComicDownloader
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;

    public VgComicDownloader(IHttpClientFactory clientFactory, IConfiguration configuration)
    {
        _clientFactory = clientFactory;
        _configuration = configuration;
    }

    public async Task<Immutable<byte[]>> Download()
    {
        var httpClient = _clientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Cookie", new []{ "SP_ID=" + _configuration.GetValue<string>("vg:cookie")});
        httpClient.DefaultRequestHeaders.Add("User-Agent", new []{"Mozilla/5.0 (Windows NT 10.0; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0"});

        var bytes = await httpClient.GetByteArrayAsync($"https://www.vg.no/tegneserier/api/images/{this.GetPrimaryKeyString()}/{DateTime.Now:yyyy-MM-dd}");
        return bytes.AsImmutable();
    }
}