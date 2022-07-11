using System.Reflection;
using comic_downloader_orleans.Downloader;
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
    c.UseLocalhostClustering()
        .AddMemoryGrainStorageAsDefault()
        .UseInMemoryReminderService()
        .AddStartupTask(StartupTask)
        .ConfigureApplicationParts(manager => manager.AddApplicationPart(Assembly.GetExecutingAssembly()).WithReferences())
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "dev";
            options.ServiceId = "OrleansBasics";
        })
        .ConfigureLogging(logging => logging.AddConsole());
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(builder.Configuration.GetValue<string>("telegram:apikey"), _.GetService<HttpClient>()));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/telegramMessage/", async ([FromBody]Update update, IGrainFactory factory) =>
{
    if (update.Message != null)
    {
        var telegramUser = factory.GetGrain<ITelegramUser>(update.Message.Chat.Id);
        var telegramPersistance = factory.GetGrain<ITelegramPersistance>(0);

        if (update.Message.Text == "/start")
        {
            await telegramPersistance.AddUser(telegramUser);
        }
        
        if (update.Message.Text == "/stop")
        {
            await telegramPersistance.RemoveUser(telegramUser);
        }
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

        var user = grainFactory.GetGrain<ITelegramUser>(49374973);
        var persistance = grainFactory.GetGrain<ITelegramPersistance>(0);
        persistance.AddUser(user);
        
        var oneDriveAccount = grainFactory.GetGrain<IOneDriveAccount>(builder.Configuration["onedrive:username"]);
        oneDriveAccount.Initialize(builder.Configuration["onedrive:clientId"], builder.Configuration["onedrive:refreshToken"]);

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