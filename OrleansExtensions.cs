using System.Net;
using System.Net.Sockets;
using System.Reflection;
using comic_downloader_orleans.Grains;
using comic_downloader_orleans.OneDrive;
using comic_downloader_orleans.Telegram;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace comic_downloader_orleans;

public static class OrleansExtensions
{
    public static void AddOrleans(this WebApplicationBuilder builder)
    {
        builder.Host.UseOrleans(c =>
        {
            c.UseDashboard();
            c.AddApplicationInsightsTelemetryConsumer(builder.Configuration["appinsights_instrumentationkey"]);
            c.AddStartupTask(StartupTask);

            if (builder.Environment.IsDevelopment())
            {
                c.UseLocalhostClustering()
                    .AddMemoryGrainStorageAsDefault()
                    .AddMemoryGrainStorage("blobstorage")
                    .ConfigureEndpoints("localhost", 9889, 9099, AddressFamily.InterNetwork, true)
                    .UseInMemoryReminderService()
                    .ConfigureLogging(logging => logging.AddConsole());
            }
            else
            {
                var siloPort = 11111;
                var gatewayPort = 30000;

                // Is running as an App Service?
                if (builder.Configuration["WEBSITE_PRIVATE_IP"] is { } ip &&
                    builder.Configuration["WEBSITE_PRIVATE_PORTS"] is { } ports)
                {
                    var endpointAddress = IPAddress.Parse(ip);
                    var splitPorts = ports.Split(',');
                    if (splitPorts.Length < 2)
                        throw new Exception("Insufficient private ports configured.");

                    siloPort = int.Parse(splitPorts[0]);
                    gatewayPort = int.Parse(splitPorts[1]);

                    c.ConfigureEndpoints(endpointAddress, siloPort, gatewayPort);
                }
                else // Assume Azure Container Apps.
                {
                    c.ConfigureEndpoints(siloPort, gatewayPort);
                }


                var connectionString = builder.Configuration.GetValue<string>("azurestorage_connectionstring");

                c.Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "comicDownloaderCluster";
                    options.ServiceId = "ComicDownloader";
                });

                c.UseAzureStorageClustering(options => options.ConfigureTableServiceClient(connectionString));
                c.AddAzureTableGrainStorageAsDefault(options =>
                {
                    options.ConfigureTableServiceClient(
                        connectionString);
                    options.UseJson = true;
                    options.IndentJson = true;
                    options.DeleteStateOnClear = true;
                });
                c.AddAzureBlobGrainStorage("blobstorage",
                    options => { options.ConfigureBlobServiceClient(connectionString); });
                c.UseAzureTableReminderService(options => options.ConfigureTableServiceClient(connectionString));
                c.ConfigureLogging(logging => logging.AddConsole());
            }

            c.ConfigureApplicationParts(manager =>
                manager.AddApplicationPart(Assembly.GetExecutingAssembly()).WithReferences());
        });
    }

    public static Task StartupTask(IServiceProvider provider, CancellationToken token)
    {
        var grainFactory = provider.GetService<IGrainFactory>();

        if (grainFactory == null) return Task.CompletedTask;
        
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
            Save = true,
        });
        
        var swordsGrain = grainFactory.GetGrain<IComic>("Swords");
        swordsGrain.Initialize(new ComicState()
        {
            Id = "https://swordscomic.com/comic/feed/",
            Name = "Swords",
            ComicHandler = ComicHandler.Rss,
        });

        var e24Grain = grainFactory.GetGrain<IComic>("e24");
        e24Grain.Initialize(new ComicState()
        {
            Id = "https://api.e24.no/content/v1/comics/{0:yyyy}-{0:MM}-{0:dd}",
            Name = "E24",
            Save = true,
            ComicHandler = ComicHandler.Url,
        });
        
        return Task.CompletedTask;
    }
}