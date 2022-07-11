using comic_downloader_orleans.Grains;
using Orleans;
using Orleans.Concurrency;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;

namespace comic_downloader_orleans.Telegram;

public interface ITelegramPersistance : IGrainWithIntegerKey
{
    Task AddUser(ITelegramUser user);
    Task RemoveUser(ITelegramUser user);
    Task SendComic(IComicImage comicImage);
}

[StatelessWorker(1)]
public class TelegramPersistance : Grain<TelegramUserData>, ITelegramPersistance
{
    private readonly ITelegramBotClient _botClient;

    public TelegramPersistance(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    /// <inheritdoc />
    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }

    /// <inheritdoc />
    public async Task AddUser(ITelegramUser user)
    {
        State.Users.Add(user);
        await WriteStateAsync();
    }

    /// <inheritdoc />
    public async Task RemoveUser(ITelegramUser user)
    {
        State.Users.Remove(user);
        await WriteStateAsync();
    }

    /// <inheritdoc />
    public async Task SendComic(IComicImage comicImage)
    {
        var imageData = await comicImage.ImageData();
        foreach (var user in State.Users.ToList())
        {
            try
            {
                var ms = new MemoryStream(imageData.Value);
                await _botClient.SendPhotoAsync(user.GetPrimaryKeyLong(), new InputOnlineFile(ms));
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Forbidden"))
                {
                    State.Users.Remove(user);
                }
            }
        }

        await WriteStateAsync();
    }
}

public class TelegramUserData
{
    public List<ITelegramUser> Users { get; set; } = new List<ITelegramUser>();
}