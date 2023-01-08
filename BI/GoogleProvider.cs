using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace MyDrive;

public class GoogleProvider
{
    private List<(string id, GoogleAccess access)> _cache = new();
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    public GoogleProvider(IConfiguration config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
    }

    private string GoogleClientId { get => _config.GetGoogleClientId(); }
    private string GoogleClientSecret { get => _config.GetGoogleClientSecret(); }

    private StorageAccess GetConfigStorage(string id)
    {
        return new StorageAccess(_config.GetBlobStorageConnectionString(), "google.config", id);
    }

    public async Task SolidifyAccess(string code, string redirect, string id)
    {
        StringBuilder body = new StringBuilder();
        body.Append("code=" + code + "&");
        body.Append("client_id=" + GoogleClientId + "&");
        body.Append("client_secret=" + GoogleClientSecret + "&");
        body.Append("redirect_uri=" + redirect + "&");
        body.Append("grant_type=authorization_code");
        var res = await Post("https://www.googleapis.com/oauth2/v4/token", body.ToString());
        var bearer = JsonConvert.DeserializeObject<JsonBearerToken>(res);
        if (bearer == null)
        {
            throw new Exception("couldn't get bearer");
        }
        var config = new JsonGoogleConfig()
        {
            RefreshToken = bearer.refresh_token
        };
        var storage = GetConfigStorage(id);
        await storage.Delete();
        using var mem = new MemoryStream();
        using var tempWriter = new StreamWriter(mem);
        await tempWriter.WriteAsync(JsonConvert.SerializeObject(config));
        await tempWriter.FlushAsync();
        using var uploader = await storage.CreateUploadStream(mem.Length);
        using var writer = new StreamWriter(uploader);
        await writer.WriteAsync(JsonConvert.SerializeObject(config));
        await writer.FlushAsync();
    }

    public async Task<GoogleAccess?> GetAccess(string id)
    {
        var cached = _cache.FirstOrDefault(x => id.Equals(x.id)).access;
        if (cached != null)
        {
            return cached;
        }
        var refreshToken = await GetConfigStorage(id).ReadFile();
        if (refreshToken == null)
        {
            return null;
        }
        var refresh = JsonConvert.DeserializeObject<JsonGoogleConfig>(refreshToken)?.RefreshToken;
        if (refresh == null)
        {
            return null;
        }
        Func<Task<string?>> getAccessToken = new AccessTokenProvider(refresh, FetchAccessTokenFromGoogle).GetAccessToken;
        var access = new GoogleAccess(getAccessToken);
        if (await access.HasAccess())
        {
            _cache.Add((id, access));
            while (_cache.Count > 100)
            {
                _cache.RemoveAt(0);
            }
            return access;
        }
        else
        {
            return null;
        }
    }

    public string CreateRequestAccessUrl(string email)
    {
        var sb = new StringBuilder();
        sb.Append("https://accounts.google.com/o/oauth2/v2/auth?");
        sb.Append("scope=email%20profile%20https://www.googleapis.com/auth/drive&");
        sb.Append("access_type=offline&");
        sb.Append("include_granted_scopes=true&");
        sb.Append("state=googlecode&");
        sb.Append("response_type=code&");
        sb.Append("login_hint=" + email + "&");
        sb.Append("prompt=select_account&");
        sb.Append("client_id=" + GoogleClientId + "&"); // still needs to add redirect_url, but the clients adds it per its host
        return sb.ToString();
    }

    private async Task<JsonAccessToken?> FetchAccessTokenFromGoogle(string refreshToken)
    {
        Console.WriteLine("FetchAccessTokenFromGoogle");
        if (string.IsNullOrEmpty(refreshToken))
        {
            return null;
        }
        try
        {
            var body = new StringBuilder();
            body.Append("client_id=" + GoogleClientId + "&");
            body.Append("client_secret=" + GoogleClientSecret + "&");
            body.Append("refresh_token=" + refreshToken + "&");
            body.Append("grant_type=refresh_token");
            var res = await Post("https://www.googleapis.com/oauth2/v4/token", body.ToString());
            var access = JsonConvert.DeserializeObject<JsonAccessToken>(res);
            return access;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> Post(string uri, string body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Content = new ByteArrayContent(Encoding.ASCII.GetBytes(body));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        using HttpResponseMessage response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        var ans = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ans;
    }

    private class JsonAccessToken
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
    }

    private class JsonBearerToken
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
        public string id_token { get; set; }
    }

    private class JsonGoogleConfig
    {
        public string RefreshToken { get; set; }
    }

    private class AccessTokenProvider
    {
        private string _refreshToken;
        private readonly Func<string, Task<JsonAccessToken?>> _fetchAccessToken;

        private string? AccessToken { get; set; } = null;
        private DateTime AccessTokenExpiration { get; set; } = DateTime.MinValue;

        public AccessTokenProvider(string refreshToken, Func<string, Task<JsonAccessToken?>> fetchAccessToken)
        {
            this._refreshToken = refreshToken;
            this._fetchAccessToken = fetchAccessToken;
        }

        public async Task<string?> GetAccessToken()
        {
            if (AccessToken != null && AccessTokenExpiration > DateTime.UtcNow)
            {
                return AccessToken;
            }

            if (_refreshToken != null)
            {
                var access = await _fetchAccessToken(_refreshToken);
                if (access != null)
                {
                    this.AccessToken = access.access_token;
                    this.AccessTokenExpiration = DateTime.UtcNow + TimeSpan.FromSeconds(access.expires_in - 10);
                    return AccessToken;
                }
            }
            return null;
        }
    }
}