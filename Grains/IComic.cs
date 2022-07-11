using comic_downloader_orleans.Downloader;
using comic_downloader_orleans.OneDrive;
using comic_downloader_orleans.Telegram;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace comic_downloader_orleans.Grains;

public interface IComic : IGrainWithStringKey
{

    Task Initialize(ComicState state);
}

public class Comic : Grain<ComicState>, IComic, IRemindable
{
    private readonly ILogger<Comic> _logger;

    public Comic(ILogger<Comic> logger)
    {
        _logger = logger;
    }

    public override async Task OnActivateAsync()
    {
        await SetupReminder();
    }

    private async Task SetupReminder()
    {
        var reminderName = this.GetPrimaryKeyString();

        var firstTick = DateTime.Today.AddHours(12).ToUniversalTime(); // start the reminder at 8pm EST
        //var firstTick = DateTime.Now.AddSeconds(5).ToUniversalTime();
        if (firstTick < DateTime.UtcNow)
        {
            // if the next start time has already passed, increase the startTime by a day
            firstTick = firstTick.AddDays(1);
        }

        var nextFirstTick = firstTick - DateTime.UtcNow; // aka dueTime
        //var interval = TimeSpan.FromDays(1); // aka period
        var interval = TimeSpan.FromMinutes(1);

        await RegisterOrUpdateReminder(reminderName, nextFirstTick, interval);
    }


    public async Task Initialize(ComicState state)
    {
        State.ComicHandler = state.ComicHandler;
        State.Url = state.Url;
        State.Id = state.Id;
        State.Name = state.Name;
        await WriteStateAsync();
    }

    public Task ReceiveReminder(string reminderName, TickStatus status)
    {
        _ = OnDownloadComic();
        return Task.CompletedTask;
    }

    private async Task OnDownloadComic()
    {
        _logger.LogInformation("Reminder called");
        try
        {
            IComicDownloader downloader = State.ComicHandler switch
            {
                ComicHandler.VG => GrainFactory.GetGrain<IVgComicDownloader>(State.Id),
                ComicHandler.TU => GrainFactory.GetGrain<ITuDownloader>(0),
                ComicHandler.Xkcd => GrainFactory.GetGrain<IXkcdDownloader>(0),
                _ => GrainFactory.GetGrain<IVgComicDownloader>(State.Id),
            };
            
            var data = await downloader.Download();

            var comicImage = GrainFactory.GetGrain<IComicImage>(Guid.NewGuid());
            await comicImage.SetData(data, State.Id);

            var hash = await comicImage.Hash();
            if (State.Hashes.Contains(hash))
            {
                await comicImage.Delete();
                return;
            }

            State.Hashes.Add(hash);
            State.Images.Add(comicImage);
            await WriteStateAsync();
        
            var telegramPersistance = GrainFactory.GetGrain<ITelegramPersistance>(0);
            await telegramPersistance.SendComic(comicImage);

            var oneDrive = GrainFactory.GetGrain<IOneDrive>(0);
            await oneDrive.SendComic(State.Name, comicImage);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while doing reminder");
        }
    }
}

public class ComicState
{
    public string Url { get; set; }
    public string Name { get; set; }
    public string Id { get; set; }
    public ComicHandler ComicHandler { get; set; }
    public List<IComicImage> Images { get; set; } = new List<IComicImage>();

    public HashSet<string> Hashes = new();
}

public enum ComicHandler
{
    VG = 1,
    TU = 2,
    Xkcd = 3,
}