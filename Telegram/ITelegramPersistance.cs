using comic_downloader_orleans.Grains;
using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("/telegram/message")]
    Task OnMessage(Update update);
}

[StatelessWorker(1)]
public class TelegramPersistance : Grain<TelegramUserData>, ITelegramPersistance
{
    private readonly IHttpClientFactory _clientFactory;

    public TelegramPersistance(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
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
        var client = new TelegramBotClient(State.ApiKey, _clientFactory.CreateClient());
        var imageData = await comicImage.ImageData();
        foreach (var user in State.Users.ToList())
        {
            try
            {
                var ms = new MemoryStream(imageData.Value);
                await client.SendPhotoAsync(user.GetPrimaryKeyLong(), new InputOnlineFile(ms));
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

    /// <inheritdoc />
    public async Task OnMessage(Update update)
    {
        if (update.Message != null)
        {
            var telegramUser = GrainFactory.GetGrain<ITelegramUser>(update.Message.Chat.Id);

            if (update.Message.Text == "/start") await AddUser(telegramUser);

            if (update.Message.Text == "/stop") await RemoveUser(telegramUser);
        }
    }
}

public class TelegramUserData
{
    public string ApiKey { get; set; }
    public List<ITelegramUser> Users { get; set; } = new List<ITelegramUser>();
}

public class Update
{
    public Message Message { get; set; }
}

public class Message
{
    public Chat Chat { get; set; }
    public string Text { get; set; }
}

public class Chat
{
    public long Id { get; set; }
}