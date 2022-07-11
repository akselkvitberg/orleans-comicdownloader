using comic_downloader_orleans.Grains;
using Orleans;
using Orleans.Concurrency;

namespace comic_downloader_orleans.OneDrive;

public interface IOneDrive : IGrainWithIntegerKey
{
    Task AddAccount(IOneDriveAccount account);
    Task RemoveAccount(IOneDriveAccount account);

    Task SendComic(string name, IComicImage comicImage);
}

[StatelessWorker(1)]
public class OneDrive : Grain<OneDriveState>, IOneDrive
{
    private readonly ILogger<OneDrive> _logger;

    public OneDrive(ILogger<OneDrive> logger)
    {
        _logger = logger;
    }
    
    
    /// <inheritdoc />
    public Task AddAccount(IOneDriveAccount account)
    {
        State.Accounts.Add(account);
        WriteStateAsync();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAccount(IOneDriveAccount account)
    {
        State.Accounts.Remove(account);
        WriteStateAsync();
        return Task.CompletedTask;
    }

    public async Task SendComic(string name, IComicImage comicImage)
    {
        var imageData = await comicImage.ImageData();
        var fileName = await comicImage.FileName();
        
        foreach (var user in State.Accounts.ToList())
        {
            await user.UploadFile(name, fileName, imageData);
        }
    }
}

public class OneDriveState
{
    public List<IOneDriveAccount> Accounts { get; set; } = new List<IOneDriveAccount>();
}