using Orleans;
using Telegram.Bot;

namespace comic_downloader_orleans.Telegram;

public class TelegramUser : Grain, ITelegramUser
{
    public TelegramUser(TelegramBotClient telegramClient)
    {
    }
}

public interface ITelegramUser : IGrainWithIntegerKey
{
    
}