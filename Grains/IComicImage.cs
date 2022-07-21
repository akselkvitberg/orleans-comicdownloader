using System.Security.Cryptography;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace comic_downloader_orleans.Grains;

public interface IComicImage : IGrainWithGuidKey
{
    Task Delete();

    Task<string> Hash();
    Task<string> FileName();
    Task SetData(Immutable<byte[]> data, string source);

    Task<Immutable<byte[]>> ImageData();
}

public class ComicImage : Grain<ComicImageData>, IComicImage
{
    private readonly IPersistentState<ImageDataState> _imageData;

    public ComicImage([PersistentState("imagedata", "blobstorage")]IPersistentState<ImageDataState> imageData)
    {
        _imageData = imageData;
    }
    
    public Task Delete()
    {
        ClearStateAsync();
        return Task.CompletedTask;
    }

    public Task<string> Hash() => Task.FromResult(State.Hash);
    public Task<string> FileName() => Task.FromResult(State.FileName);

    public async Task SetData(Immutable<byte[]> data, string source)
    {
        _imageData.State.Bytes = data.Value;
        await _imageData.WriteStateAsync();
        State.Hash = GenerateHash(data.Value);
        State.Source = source;
        State.Date = DateOnly.FromDateTime(DateTime.Now);
        State.FileName = GetFileName(data.Value);

        await WriteStateAsync();
    }

    public Task<Immutable<byte[]>> ImageData() => Task.FromResult(_imageData.State.Bytes.AsImmutable());

    private string GenerateHash(byte[] data)
    {
        var hashData = MD5.HashData(data);
        return Convert.ToBase64String(hashData);
    }
    
    public static string GetFileName(byte[] data)
    {
        var jpg = new[] { "FF", "D8" };
        var bmp = new[] { "42", "4D" };
        var gif = new[] { "47", "49", "46" };
        var png = new[] { "89", "50", "4E", "47", "0D", "0A", "1A", "0A" };

        var extension = ".png";
        if (TestFormat(jpg, data))
        {
            extension = ".jpg";
        }
        else if (TestFormat(png, data))
        {
            extension = ".png";
        }
        else if (TestFormat(gif, data))
        {
            extension = ".gif";
        }
        else if (TestFormat(bmp, data))
        {
            extension = ".bmp";
        }

        return DateTime.Now.ToString("yyyy.MM.dd") + extension;
    }
    
    private static bool TestFormat(string[] magic, byte[] data)
    {
        if (data.Length < magic.Length)
            return false;

        for (int i = 0; i < magic.Length; i++)
        {
            if (magic[i] != data[i].ToString("X2"))
            {
                return false;
            }
        }

        return true;
    }
}

public class ImageDataState
{
    public byte[] Bytes { get; set; }
}

public class ComicImageData
{
    public string Hash { get; set; }
    public DateOnly Date { get; set; }
    public string Source { get; set; }
    public string FileName { get; set; }
}