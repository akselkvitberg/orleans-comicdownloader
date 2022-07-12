using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Graph;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace comic_downloader_orleans.OneDrive;

public class OneDriveAccount : Grain<OneDriveAccountState>, IOneDriveAccount, IRemindable
{
    private readonly ILogger<OneDriveAccount> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private string _accessToken;

    public OneDriveAccount(ILogger<OneDriveAccount> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public override async Task OnActivateAsync()
    {
        if (State.RefreshToken != null)
        {
            await RefreshToken();
            await RegisterOrUpdateReminder("OneDriveRefreshToken", TimeSpan.FromDays(1), TimeSpan.FromDays(1));
        }
    }

    public async Task UploadFile(string folderName, string fileName, Immutable<byte[]> bytes)
    {
        var client = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            return Task.CompletedTask;
        }));
        
        try
        {
            using var ms = new MemoryStream(bytes.Value);
            var totalLength = bytes.Value.Length;

            if (totalLength > 4 * 1024 * 1024)
            {
                var request = client.Me.Drive.Special.AppRoot.ItemWithPath($"{folderName}/{fileName}")
                    .CreateUploadSession().Request();

                var uploadSession = await request.PostAsync();
                int maxSliceSize = 320 * 1024;
                var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, ms, maxSliceSize);

                await fileUploadTask.UploadAsync();
            }
            else
            {
                var request = client.Me.Drive.Special.AppRoot.ItemWithPath($"{folderName}/{fileName}").Content
                    .Request();
                await request.PutAsync<DriveItem>(ms);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not upload comic image to onedrive");
        }
    }

    private async Task RefreshToken()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var body = $"client_id={State.ClientId}&refresh_token={State.RefreshToken}&redirect_uri=http://localhost:8000&grant_type=refresh_token";
            var result = await client.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded"));
            var json = await result.Content.ReadAsStringAsync();
            
            var node = JsonNode.Parse(json);
            State.RefreshToken = node["refresh_token"].GetValue<string>();
            _accessToken = node["access_token"].GetValue<string>();
            await WriteStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not refresh token - cannot save to OneDrive");
        }
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        await RefreshToken();
    }

    /// <inheritdoc />
    public async Task Initialize(string clientId, string refreshToken)
    {
        State.RefreshToken = refreshToken;
        State.ClientId = clientId;
        await RefreshToken();
        await RegisterOrUpdateReminder("OneDriveRefreshToken", TimeSpan.FromDays(1), TimeSpan.FromDays(1));
    }
}

public class OneDriveAccountState
{
    public string RefreshToken { get; set; }
    public string ClientId { get; set; }
}

public interface IOneDriveAccount : IGrainWithStringKey
{
    Task Initialize(string clientId, string refreshToken);
    Task UploadFile(string folderName, string fileName, Immutable<byte[]> bytes);
}