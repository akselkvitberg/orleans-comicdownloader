using Orleans.Concurrency;

namespace comic_downloader_orleans.Downloader;

public interface IComicDownloader
{
    Task<Immutable<byte[]>> Download();
}