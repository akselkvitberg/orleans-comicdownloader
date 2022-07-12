using System.Net.Sockets;
using System.Reflection;
using comic_downloader_orleans.Grains;
using comic_downloader_orleans.OneDrive;
using comic_downloader_orleans.Telegram;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Telegram.Bot;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Host.UseOrleans(c =>
{
    c.UseDashboard()
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "comicDownloader";
            options.ServiceId = "ComicDownloader";
        });

    if (builder.Environment.IsDevelopment())
        c.UseLocalhostClustering()
            .AddMemoryGrainStorageAsDefault()
            .ConfigureEndpoints("localhost", 9889, 9099, AddressFamily.InterNetwork, true)
            .UseInMemoryReminderService()
            .AddStartupTask(StartupTask)
            .ConfigureLogging(logging => logging.AddConsole());
    
    else
    {
        c.UseAzureStorageClustering(options => options.ConfigureTableServiceClient(builder.Configuration.GetValue<string>("azurestorage:connectionstring")));
        c.AddAzureTableGrainStorageAsDefault(options =>
        {
            options.ConfigureTableServiceClient(
                builder.Configuration.GetValue<string>("azurestorage:connectionstring"));
            options.UseJson = true;
            options.IndentJson = true;
            options.DeleteStateOnClear = true;
        });
        c.UseAzureTableReminderService(options => options.ConfigureTableServiceClient(builder.Configuration.GetValue<string>("azurestorage:connectionstring")));
    }

    c.ConfigureApplicationParts(manager =>
        manager.AddApplicationPart(Assembly.GetExecutingAssembly()).WithReferences());
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ITelegramBotClient>(_ =>
    new TelegramBotClient(builder.Configuration.GetValue<string>("telegram:apikey"), _.GetService<HttpClient>()));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/telegramMessage/", async ([FromBody] Update update, IGrainFactory factory) =>
{
    if (update.Message != null)
    {
        var telegramUser = factory.GetGrain<ITelegramUser>(update.Message.Chat.Id);
        var telegramPersistance = factory.GetGrain<ITelegramPersistance>(0);

        if (update.Message.Text == "/start") await telegramPersistance.AddUser(telegramUser);

        if (update.Message.Text == "/stop") await telegramPersistance.RemoveUser(telegramUser);
    }
});

app.Run();

Task StartupTask(IServiceProvider provider, CancellationToken token)
{
    var grainFactory = provider.GetService<IGrainFactory>();

    if (grainFactory != null)
    {
        var pondusGrain = grainFactory.GetGrain<IComic>("Vg_Pondus");
        pondusGrain.Initialize(new ComicState()
        {
            Id = "pondus",
            Name = "Pondus",
            ComicHandler = ComicHandler.VG,
        });
        
        var gjesteGrain = grainFactory.GetGrain<IComic>("Vg_Gjesteserie");
        gjesteGrain.Initialize(new ComicState()
        {
            Id = "gjesteserie",
            Name = "VG Gjesteserie",
            ComicHandler = ComicHandler.VG,
        });
        
        var hjalmarGrain = grainFactory.GetGrain<IComic>("Vg_Hjalmar");
        hjalmarGrain.Initialize(new ComicState()
        {
            Id = "hjalmar",
            Name = "Hjalmar",
            ComicHandler = ComicHandler.VG,
        });
        
        var lunchGrain = grainFactory.GetGrain<IComic>("Vg_lunch");
        lunchGrain.Initialize(new ComicState()
        {
            Id = "lunch",
            Name = "Lunch VG",
            ComicHandler = ComicHandler.VG,
        });
        
        var hannelandGrain = grainFactory.GetGrain<IComic>("Vg_hanneland");
        hannelandGrain.Initialize(new ComicState()
        {
            Id = "hanneland",
            Name = "Tegnehanne",
            ComicHandler = ComicHandler.VG,
        });
        
        var storefriGrain = grainFactory.GetGrain<IComic>("Vg_storefri");
        storefriGrain.Initialize(new ComicState()
        {
            Id = "storefri",
            Name = "Storefri",
            ComicHandler = ComicHandler.VG,
        });
        
        var xkcdGrain = grainFactory.GetGrain<IComic>("xkcd");
        xkcdGrain.Initialize(new ComicState()
        {
            Id = "xkcd",
            Name = "xkcd",
            ComicHandler = ComicHandler.Xkcd,
            Save = false,
        });
        
        var lunchTuGrain = grainFactory.GetGrain<IComic>("Tu_lunch");
        lunchTuGrain.Initialize(new ComicState()
        {
            Id = "lunch",
            Name = "Lunch TU",
            ComicHandler = ComicHandler.TU,
        });
        
        var calvinGrain = grainFactory.GetGrain<IComic>("Calvin-Hobbes");
        calvinGrain.Initialize(new ComicState()
        {
            Id = "https://www.comicsrss.com/rss/calvinandhobbes.rss",
            Name = "Calvin and Hobbes",
            ComicHandler = ComicHandler.Rss,
            Save = true
        });
        
        var swordsGrain = grainFactory.GetGrain<IComic>("Swords");
        swordsGrain.Initialize(new ComicState()
        {
            Id = "https://swordscomic.com/comic/feed/",
            Name = "Swords",
            ComicHandler = ComicHandler.Rss,
        });
        

        var user = grainFactory.GetGrain<ITelegramUser>(49374973);
        var persistance = grainFactory.GetGrain<ITelegramPersistance>(0);
        persistance.AddUser(user);

        var oneDriveAccount = grainFactory.GetGrain<IOneDriveAccount>(builder.Configuration["onedrive:username"]);
        oneDriveAccount.Initialize(builder.Configuration["onedrive:clientId"],
            builder.Configuration["onedrive:refreshToken"]);

        var oneDrive = grainFactory.GetGrain<IOneDrive>(0);
        oneDrive.AddAccount(oneDriveAccount);
    }

    return Task.CompletedTask;
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