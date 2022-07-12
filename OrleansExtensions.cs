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
            c.UseDashboard()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "comicDownloader";
                    options.ServiceId = "ComicDownloader";
                });

            if (builder.Environment.IsDevelopment())
            {
                c.UseLocalhostClustering()
                    .AddMemoryGrainStorageAsDefault()
                    .ConfigureEndpoints("localhost", 9889, 9099, AddressFamily.InterNetwork, true)
                    .UseInMemoryReminderService()
                    .AddStartupTask(StartupTask)
                    .ConfigureLogging(logging => logging.AddConsole());
            }
            else
            {
                var endpointAddress = IPAddress.Parse(builder.Configuration["WEBSITE_PRIVATE_IP"]);
                var strPorts = builder.Configuration["WEBSITE_PRIVATE_PORTS"].Split(',');
                if (strPorts.Length < 2)
                    throw new Exception("Insufficient private ports configured.");
                var (siloPort, gatewayPort) =
                    (int.Parse(strPorts[0]), int.Parse(strPorts[1]));
                

                c.ConfigureEndpoints(endpointAddress, siloPort, gatewayPort);
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
                c.ConfigureLogging(logging => logging.AddConsole());
            }

            c.ConfigureApplicationParts(manager =>
                manager.AddApplicationPart(Assembly.GetExecutingAssembly()).WithReferences());
        });
    }

    public static Task StartupTask(IServiceProvider provider, CancellationToken token)
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

            var configuration = provider.GetService<IConfiguration>();

            var user = grainFactory.GetGrain<ITelegramUser>(configuration.GetValue<long>("telegram:initialUser"));
            var persistance = grainFactory.GetGrain<ITelegramPersistance>(0);
            persistance.AddUser(user);

            var oneDriveAccount = grainFactory.GetGrain<IOneDriveAccount>(configuration["onedrive:username"]);
            oneDriveAccount.Initialize(configuration["onedrive:clientId"], configuration["onedrive:refreshToken"]);

            var oneDrive = grainFactory.GetGrain<IOneDrive>(0);
            oneDrive.AddAccount(oneDriveAccount);
        }

        return Task.CompletedTask;
    }
}