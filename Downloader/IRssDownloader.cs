using System.Text.RegularExpressions;
using System.Xml.Linq;
using Orleans;
using Orleans.Concurrency;

namespace comic_downloader_orleans.Downloader;

public interface IRssDownloader : IComicDownloader, IGrainWithStringKey
{
}

public class RssDownloader : Grain, IRssDownloader
{
    private readonly IHttpClientFactory _factory;

    public RssDownloader(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<Immutable<byte[]>> Download()
    {
        var httpClient = _factory.CreateClient();

        var xml = await httpClient.GetStringAsync(this.GetPrimaryKeyString());
        var doc = XElement.Parse(xml);
        var innerXml = doc.Element("channel").Element("item").Element("description").Value;
        var imageUrl = Regex.Match(innerXml, @"img.*src=""(\S+)""").Groups[1].Value;

        var bytes = await httpClient.GetByteArrayAsync(imageUrl);

        return bytes.AsImmutable();
    }
}