using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace MyDrive;

public class GoogleProvider
{
    private readonly IConfiguration _config;
    public GoogleProvider(IConfiguration config)
    {
        _config = config;
    }

    private string GoogleClientId { get => _config.GetGoogleClientId(); }
    private string GoogleClientSecret { get => _config.GetGoogleClientSecret(); }

    private StorageAccess GetConfigStorage(string authId)
    {
        return new StorageAccess(_config.GetBlobStorageConnectionString(), "google.config", authId);
    }

    internal async Task SolidifyAccess(string code, string redirect, string authId)
    {
        StringBuilder body = new StringBuilder();
        body.Append("code=" + code + "&");
        body.Append("client_id=" + GoogleClientId + "&");
        body.Append("client_secret=" + GoogleClientSecret + "&");
        body.Append("redirect_uri=" + redirect + "&");
        body.Append("grant_type=authorization_code");
        var res = await Post("https://www.googleapis.com/oauth2/v4/token", body.ToString());
        var bearer = JsonConvert.DeserializeObject<JsonGoogleBearer>(res.body);
        var storage = GetConfigStorage(authId);
        await storage.Delete();
        var config = new JsonGoogleConfig()
        {
            RefreshToken = bearer.refresh_token
        };
        using (var mem = new MemoryStream())
        using (var tempWriter = new StreamWriter(mem))
        {
            await tempWriter.WriteAsync(JsonConvert.SerializeObject(config));
            await tempWriter.FlushAsync();
            using (var uploader = await storage.CreateUploadStream(mem.Length))
            using (var writer = new StreamWriter(uploader))
            {
                await writer.WriteAsync(JsonConvert.SerializeObject(config));
                await writer.FlushAsync();
            }
        }
    }

    public async Task<string> GetAccessToken(string authId)
    {
        var refreash = JsonConvert.DeserializeObject<JsonGoogleConfig>(await GetConfigStorage(authId).ReadFile()).RefreshToken;
        var access = await FetchAccessTokenFromGoogle(refreash);
        return access;
    }

    public string CreateRequestAccessUrl(string email)
    {
        var sb = new StringBuilder();
        sb.Append("https://accounts.google.com/o/oauth2/v2/auth?");
        sb.Append("scope=email%20profile%20https://www.googleapis.com/auth/drive&");
        sb.Append("access_type=offline&");
        sb.Append("include_granted_scopes=true&");
        sb.Append("response_type=code&");
        sb.Append("login_hint=" + email + "&");
        sb.Append("prompt=select_account&");
        sb.Append("client_id=" + GoogleClientId + "&");
        return sb.ToString();
    }

    private async Task<string> FetchAccessTokenFromGoogle(string refreshToken)
    {
        try
        {
            var body = new StringBuilder();
            body.Append("client_id=" + GoogleClientId + "&");
            body.Append("client_secret=" + GoogleClientSecret + "&");
            body.Append("refresh_token=" + refreshToken + "&");
            body.Append("grant_type=refresh_token");
            var res = await Post("https://www.googleapis.com/oauth2/v4/token", body.ToString());
            var access = JsonConvert.DeserializeObject<JsonAccessToken>(res.body);
            return access.access_token;
        }
        catch
        {
            return null;
        }
    }

    private async Task<(WebResponse response, string body)> Post(string uri, string body)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        request.Method = "POST";
        var data = Encoding.ASCII.GetBytes(body);
        request.ContentType = "application/x-www-form-urlencoded";
        request.ContentLength = data.Length;
        using (var stream = await request.GetRequestStreamAsync())
        {
            stream.Write(data, 0, data.Length);
        }
        try
        {
            var response = (HttpWebResponse)await request.GetResponseAsync();
            string responseString = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();
            return (response, responseString);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private class JsonAccessToken
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
    }

    private class JsonGoogleBearer
    {
        public string access_token { get; set; }
        public string expires_in { get; set; }
        public string token_type { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
        public string id_token { get; set; }
    }

    private class JsonGoogleConfig
    {
        public string RefreshToken { get; set; }
    }
}