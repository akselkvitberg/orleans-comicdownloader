using comic_downloader_orleans;
using Orleans.Http;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.AddOrleans();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(builder.Configuration.GetValue<string>("telegram:apikey"), _.GetService<HttpClient>()));

builder.Services.AddGrainRouter();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapGrains();
    endpoints.MapHealthChecks("health");
});

app.Run();
