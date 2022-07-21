using comic_downloader_orleans;
using Orleans;
using Orleans.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddOrleans();

builder.Services.AddHttpClient();

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
app.UseOrleansDashboard();

app.Run();
