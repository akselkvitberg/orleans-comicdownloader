using Orleans;
using Orleans.Concurrency;

namespace comic_downloader_orleans.Downloader;

public interface IVgComicDownloader : IComicDownloader, IGrainWithStringKey
{
    
}


public class VgComicDownloader : Grain, IVgComicDownloader
{
    private readonly IHttpClientFactory _clientFactory;

    public VgComicDownloader(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<Immutable<byte[]>> Download()
    {
        var vgCookie = await GrainFactory.GetGrain<VgCookie>(0).Cookie();
        
        var httpClient = _clientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Cookie", new []{ "SP_ID=" + vgCookie});
        httpClient.DefaultRequestHeaders.Add("User-Agent", new []{"Mozilla/5.0 (Windows NT 10.0; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0"});

        var bytes = await httpClient.GetByteArrayAsync($"https://www.vg.no/tegneserier/api/images/{this.GetPrimaryKeyString()}/{DateTime.Now:yyyy-MM-dd}");
        return bytes.AsImmutable();
    }
}

public class VgCookie : Grain<VgState>, IVgCookie
{
    /// <inheritdoc />
    public override async Task OnActivateAsync()
    {
        await WriteStateAsync();
    }

    /// <inheritdoc />
    public Task<string> Cookie()
    {
        return Task.FromResult(State.Cookie);
    }
}

public interface IVgCookie : IGrainWithIntegerKey
{
    Task<string> Cookie();
}

public class VgState
{
    public string Cookie { get; set; }
}